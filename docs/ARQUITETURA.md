# WAVE — Arquitetura

**WAVE** — *Wi-Fi Automated Validation Environment*. Ferramenta desktop para **homologação automatizada de conectividade Wi-Fi em tablets Windows**, conforme `especificacao_tecnica_wifi.pdf`.

O operador vê botões por rede (SSID). Ao tocar, o sistema conecta na rede (criando o perfil WPA2/WPA3 se preciso), aguarda IP via DHCP e dispara três rotinas de validação — ping contínuo, teste de velocidade (fast.com) e vídeo de streaming (YouTube) — enquanto exibe telemetria e registra o resultado para auditoria.

## 1. Decisões de tecnologia

| Item | Decisão | Motivo |
|------|---------|--------|
| Framework | **WPF (.NET 8)** | Alinhado à spec; XAML+C#; acesso nativo a `netsh`/Native Wi-Fi, `Process` e DPAPI. |
| Distribuição | **`.exe` self-contained single-file** (x64 e ARM64) | Roda em qualquer Windows (tablet + desktop) sem instalar .NET. |
| UI | **MVVM** (CommunityToolkit.Mvvm) | Separa front de back; ViewModels testáveis sem XAML. |
| DI | **Microsoft.Extensions.DependencyInjection + Hosting** | Composição única, baixo acoplamento (DIP). |
| Persistência | **JSON local** + **DPAPI** para credenciais | Leve, sem instalador de banco; segredos nunca em texto claro. |

> Android/`.apk` não é viável para este fluxo (as automações são Windows-nativas). O núcleo (`Domain`/`Application`) é independente de UI e poderia ser reaproveitado por um futuro front Android nas partes suportadas.

## 2. Separação back / front e módulos

Cinco projetos, dependências apontando sempre para dentro (Clean Architecture):

```
WAVE.App  ──►  WAVE.Infrastructure  ──►  WAVE.Application  ──►  WAVE.Domain
   (front/WPF)        (Windows)              (regras/uso)         (núcleo puro)
        └───────────────────────────────────────────────────────────┘
                         (App referencia todos p/ compor a DI)

WAVE.UnitTests  ──►  Application + Domain (lógica pura, sem Windows)
```

- **WAVE.Domain** — modelos, enums e *value objects* puros. Sem dependências externas. (back)
- **WAVE.Application** — abstrações (interfaces), o orquestrador/máquina de estados e cálculos puros. Não conhece Windows nem XAML. (back)
- **WAVE.Infrastructure** — implementações Windows das abstrações (netsh, processos, ping, navegador, DPAPI, JSON). (back)
- **WAVE.App** — WPF: Views, componentes reutilizáveis, ViewModels, composição de DI. (front)
- **WAVE.UnitTests** — testa a lógica pura da Application/Domain.

## 3. Design patterns aplicados (sem overengineering)

- **MVVM** — front desacoplado; `MainViewModel`, `NetworkButtonViewModel`, `TelemetryViewModel`, `HistoryViewModel`.
- **State / máquina de estados** — `TestOperationState` (Idle → Connecting → TestRunning → Failed) dirigida pelo `WifiTestOrchestrator`, implementando a pseudológica da spec.
- **Strategy** — cada rotina de teste (ping visível, velocidade, streaming) atrás de uma interface, orquestradas de forma intercambiável.
- **Repository** — `INetworkProfileRepository`, `ITestRunRepository` isolam a persistência.
- **Factory** — `IWifiProfileFactory`/`WlanProfileXmlBuilder` monta o XML de perfil WLAN.
- **Dependency Injection (DIP)** — tudo depende de abstração; a implementação é injetada na composição.
- **Observer** — telemetria de ping como fluxo de eventos que a UI observa (`INotifyPropertyChanged`).
- **Result (railway)** — `Result`/`Result<T>` para fluxo previsível sem exceções de controle.

## 4. Segurança e permissionamento (RBAC)

Atende a regra 4 de `RegrasPrimordiaisDeDesenvolvimento.md`. Gerenciar perfis e credenciais Wi-Fi é operação sensível.

- **Papéis**: `Operator` (executa testes, vê histórico) e `Administrator` (tudo + gerenciar perfis/credenciais + editar configurações).
- **Permissões**: `RunTest`, `ViewHistory`, `ManageProfiles`, `EditSettings`.
- **Autorização em todas as camadas**: o `AuthorizationService` é validado **na Application** (no orquestrador e nos serviços de perfil), não só na UI — a UI apenas esconde/desabilita o que o papel não pode.
- **Menor privilégio**: o app inicia como `Operator`; ações administrativas exigem elevação explícita de papel.
- **Credenciais**: chaves WPA2/WPA3 cifradas com **DPAPI** (`ProtectedData`), nunca em texto claro; o XML de perfil é gerado em memória e não é persistido com a chave.
- **Validação de entrada**: SSIDs/credenciais validados antes de montar comandos; parâmetros de `netsh`/URL tratados para evitar injeção de argumentos.
- **Exceções**: tratadas sem vazar detalhes internos ao operador (mensagens amigáveis; detalhes vão para log técnico).

## 5. Mapeamento spec → código

| Spec | Implementação |
|------|---------------|
| Botões por SSID (min 60×60, touch) | `Controls/NetworkButton` + `NetworkButtonViewModel` |
| Estados IDLE/CONNECTING/TEST_RUNNING/FAILED + cores | `TestOperationState` + `StateToBrushConverter` + estilos |
| Bloqueio de reentrância | `WifiTestOrchestrator` rejeita run concorrente; ViewModel desabilita botões |
| Criar perfil se não existir | `WlanProfileXmlBuilder` + `NetshWifiConnector.EnsureProfileAsync` |
| Conectar via Windows | `NetshWifiConnector` (`netsh wlan connect`) |
| Timeout DHCP 15s | `DhcpAddressValidator` + `TestRunnerOptions.DhcpTimeout` |
| Encerrar só o que o WAVE abriu (janela de ping, por PID) | `SystemProcessTerminator` (escopo por PID) + `VisiblePingTerminal` |
| Ping contínuo visível | `VisiblePingTerminalLauncher` (`cmd /c start ping -t`) |
| Latência ao vivo no app | `ContinuousPingService` (.NET `Ping`) → `PingLatencyChart` |
| fast.com anônimo | `FastComSpeedTestLauncher` (Edge `--inprivate`) |
| YouTube alta qualidade anônimo | `YouTubeStreamingLauncher` (URL configurável) |
| Layout retrato/paisagem | `Layout/ResponsiveSplitView` |
| Auditoria | `ITestRunRepository` + `HistoryViewModel` |

Valores como timeouts e URLs ficam em `TestRunnerOptions`/configuração — sem *magic numbers* nem hardcodes (regra 3).

## 6. Como construir

Ver `README.md` na raiz. Resumo:

```powershell
dotnet build
dotnet test
dotnet publish src/WAVE.App -c Release -r win-x64   --self-contained true -p:PublishSingleFile=true
dotnet publish src/WAVE.App -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true
```
