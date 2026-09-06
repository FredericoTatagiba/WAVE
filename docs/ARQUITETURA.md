# WAVE — Arquitetura

**WAVE** — *Wi-Fi Automated Validation Environment*. Ferramenta desktop para **homologação automatizada de conectividade Wi-Fi em tablets Windows**, conforme `especificacao_tecnica_wifi.pdf`.

O operador vê botões por rede (SSID). Ao tocar, o sistema conecta na rede (criando o perfil WPA2/WPA3 se preciso), aguarda IP via DHCP e dispara três rotinas de validação — ping contínuo, teste de velocidade (fast.com) e vídeo de streaming (YouTube) — enquanto exibe telemetria e registra o resultado para auditoria.

## 1. Decisões de tecnologia

| Item | Decisão | Motivo |
|------|---------|--------|
| Framework | **Avalonia 11 (.NET 8)** | XAML+C# como o WPF, com o mesmo modelo MVVM, mas rodando em Windows e Linux a partir de uma única árvore de UI. |
| Distribuição | **binário self-contained single-file** (win-x64, win-arm64, linux-x64) | Roda sem instalar .NET; o público-alvo é técnico em campo, não quem administra runtimes. |
| UI | **MVVM** (CommunityToolkit.Mvvm) | Separa front de back; ViewModels testáveis sem XAML. |
| DI | **Microsoft.Extensions.DependencyInjection + Hosting** | Composição única, baixo acoplamento (DIP). |
| Persistência | **JSON local**; credenciais sob **DPAPI** (Windows) ou **AES-GCM** (Linux) | Leve, sem instalador de banco; segredos nunca em texto claro. libsecret foi descartado por depender de keyring vivo em sessão D-Bus, que não existe via SSH nem em quiosque. |
| Wi-Fi | **netsh + wlanapi** (Windows), **nmcli** (Linux) | São as interfaces nativas de cada SO. Ambas por trás das mesmas abstrações; o único `if` de plataforma vive no composition root. |

> macOS está fora de escopo: desde o Sonoma 14.4 não há caminho estável de linha de comando para escanear redes sem elevação (`airport` foi removido), o que exigiria um binding nativo de CoreWLAN.
>
> Android/`.apk` continua inviável para este fluxo. O núcleo (`Domain`/`Application`) é independente de UI e poderia ser reaproveitado por um futuro front Android nas partes suportadas.

## 2. Separação back / front e módulos

Cinco projetos, dependências apontando sempre para dentro (Clean Architecture):

```
WAVE.App  ──►  WAVE.Infrastructure  ──►  WAVE.Application  ──►  WAVE.Domain
 (front/Avalonia)   (Windows + Linux)         (regras/uso)         (núcleo puro)
        └───────────────────────────────────────────────────────────┘
                         (App referencia todos p/ compor a DI)

WAVE.UnitTests  ──►  Application + Domain (lógica pura, sem SO) + os exportadores
```

- **WAVE.Domain** — modelos, enums e *value objects* puros. Sem dependências externas. (back)
- **WAVE.Application** — abstrações (interfaces), o orquestrador/máquina de estados e cálculos puros. Não conhece Windows nem XAML. (back)
- **WAVE.Infrastructure** — implementações por SO das abstrações: netsh/wlanapi/DPAPI no Windows, nmcli/AES-GCM no Linux, e o que é comum aos dois (processos, ping, navegador, JSON, exportadores). (back)
- **WAVE.App** — Avalonia: Views, componentes reutilizáveis, ViewModels, composição de DI. É onde vive `AddPlatformServices`, o único ponto que ramifica por sistema operacional. (front)
- **WAVE.UnitTests** — testa a lógica pura da Application/Domain.

## 3. Design patterns aplicados (sem overengineering)

- **MVVM** — front desacoplado; `MainViewModel`, `NetworkButtonViewModel`, `TelemetryViewModel`, `HistoryViewModel`.
- **State / máquina de estados** — `TestOperationState` (Idle → Connecting → TestRunning → Failed) dirigida pelo `ConnectivityTestOrchestrator`, implementando a pseudológica da spec.
- **Strategy** — cada rotina de teste (ping visível, velocidade, streaming) atrás de uma interface, orquestradas de forma intercambiável.
- **Repository** — `INetworkProfileRepository`, `ITestRunRepository` isolam a persistência.
- **Factory** — `IWifiProfileFactory`/`WlanProfileXmlBuilder` monta o XML de perfil WLAN.
- **Dependency Injection (DIP)** — tudo depende de abstração; a implementação é injetada na composição.
- **Observer** — telemetria de ping como fluxo de eventos que a UI observa (`INotifyPropertyChanged`).
- **Result (railway)** — `Result`/`Result<T>` para fluxo previsível sem exceções de controle.

## 4. Segurança e permissionamento

Atende a regra 4 de `RegrasPrimordiaisDeDesenvolvimento.md`. Curar o catálogo de redes e suas credenciais é operação sensível; rodar um teste não é.

- **Sem login**: o app abre direto na lista de redes. Executar teste e ler histórico não exigem identidade, então não se cobra senha por eles.
- **Uma senha de administrador**, verificada no momento da ação: cadastrar/excluir rede no catálogo e editar configurações. Criada no primeiro uso e mantida desbloqueada até o app fechar (`IAdminSession`).
- **Autorização na Application**: `IAdminSession.RequireUnlocked()` é validado no `NetworkProfileService`, não só na UI — esconder um botão não é controle de acesso.
- **Limite honesto**: é um controle de aplicação, não uma fronteira criptográfica. Quem edita `settings.json` remove o hash — e esse é justamente o caminho de recuperação da senha perdida.
- **Identidade no histórico é o dispositivo**, não a pessoa (`IDeviceIdentity`). Num tablet compartilhado, login por técnico converge para conta única e o nome registrado deixa de ser confiável; auditoria por pessoa exigiria enviar o resultado para fora do dispositivo, já que o histórico é um JSON local editável.
- **Credenciais**: chaves WPA2/WPA3 cifradas com **DPAPI** (`ProtectedData`) no Windows e AES-GCM no Linux, nunca em texto claro; o XML de perfil é gerado em memória e não é persistido com a chave. O DPAPI é amarrado à conta Windows, não à senha do WAVE.
- **Validação de entrada**: SSIDs/credenciais validados antes de montar comandos; parâmetros de `netsh`/URL tratados para evitar injeção de argumentos.
- **Exceções**: tratadas sem vazar detalhes internos ao operador (mensagens amigáveis; detalhes vão para log técnico).

## 5. Mapeamento spec → código

| Spec | Implementação |
|------|---------------|
| Botões por SSID (min 60×60, touch) | `Controls/NetworkButton` + `NetworkButtonViewModel` |
| Estados IDLE/CONNECTING/TEST_RUNNING/FAILED + cores | `TestOperationState` + `StateToBrushConverter` + estilos |
| Bloqueio de reentrância | `ConnectivityTestOrchestrator` rejeita run concorrente; ViewModel desabilita botões |
| Criar perfil se não existir | `WlanProfileXmlBuilder` + `NetshWifiConnector.EnsureProfileAsync` |
| Conectar ao SO | `NetshWifiConnector` (`netsh wlan connect`) / `NmcliWifiConnector` (`nmcli connection up`) |
| Timeout DHCP 15s | `NetworkInterfaceDhcpValidator` + `TestRunnerOptions.DhcpTimeout` |
| Nenhuma janela externa durante o teste | Nada é lançado: medições rodam no processo do app |
| Ping contínuo | `ContinuousPingMonitor` (.NET `Ping`) → `PingLatencyChart` |
| Feedback da fase de conexão | `MainViewModel.IsConnecting` → `ProgressBar` no botão e na barra de status |
| fast.com anônimo | `FastComSpeedTestLauncher` (Edge `--inprivate`) |
| YouTube alta qualidade anônimo | `YouTubeStreamingLauncher` (URL configurável) |
| Layout retrato/paisagem | `Layout/ResponsiveSplitView` |
| Auditoria | `ITestRunRepository` + `HistoryViewModel` |

Valores como timeouts e URLs ficam em `TestRunnerOptions`/configuração — sem *magic numbers* nem hardcodes (regra 3).

## 6. Como construir

Ver `README.md` na raiz. Resumo:

```bash
dotnet build
dotnet test

./publish.sh              # linux-x64
# ou, no Windows:
.\publish.ps1            # win-x64 e win-arm64
```
