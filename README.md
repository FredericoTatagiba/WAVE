# WAVE — Wi-Fi Automated Validation Environment

Ferramenta desktop para **homologação automatizada de conectividade Wi-Fi em tablets Windows**, conforme `especificacao_tecnica_wifi.pdf`.

O operador vê botões por rede (SSID). Ao tocar, o sistema conecta na rede (criando o perfil WPA2/WPA3 se necessário), aguarda IP via DHCP e dispara três rotinas de validação — ping contínuo (`google.com`), teste de velocidade (fast.com) e vídeo de streaming (YouTube) — exibindo telemetria de latência ao vivo e registrando o resultado para auditoria.

## Requisitos

- **Windows 10 ou 11** (x64 ou ARM64). As automações usam APIs nativas do Windows (`netsh`, Native Wi-Fi, `Process`, DPAPI).
- **.NET 8 SDK** apenas para compilar. O `.exe` publicado é self-contained e **não exige .NET instalado** na máquina de destino.

## Compilar e testar

```powershell
dotnet build
dotnet test
```

Os testes cobrem a lógica pura (estatísticas de ping, gerador de perfil WLAN, RBAC e a máquina de estados do teste) e rodam em qualquer sistema operacional.

## Executar em desenvolvimento

```powershell
dotnet run --project src/WAVE.App
```

## Gerar o `.exe` único (self-contained)

```powershell
./publish.ps1
```

Isso gera `publish/win-x64/WAVE.exe` e `publish/win-arm64/WAVE.exe` — cada um é um **único arquivo, sem instalador**, que roda em qualquer Windows do respectivo tipo (tablet ou desktop). Ou, manualmente:

```powershell
dotnet publish src/WAVE.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

## Primeiro uso

1. Abra o `WAVE.exe`. A sessão inicia como **Operador** (menor privilégio).
2. Para cadastrar redes, digite uma senha (mín. 8 caracteres) no campo do topo e clique **Elevar acesso**. No primeiro acesso, essa senha passa a ser a **senha administrativa** (bootstrap). Nos próximos acessos ela é exigida.
3. Como **Administrador**, use o formulário *Cadastrar rede* para adicionar SSID, tipo de segurança e senha da rede (a senha é cifrada com DPAPI).
4. Volte a **Operador** e toque no botão da rede para executar o teste.

Redes **abertas** podem ser testadas sem elevação.

## Estrutura (camadas / módulos)

```
src/
  WAVE.Domain          # Núcleo puro: modelos, enums, Result (sem dependências)
  WAVE.Application     # Abstrações, RBAC, orquestrador (máquina de estados), cálculos
  WAVE.Infrastructure  # Windows: netsh, processos, ping, navegador, DPAPI, JSON
  WAVE.App             # Front WPF (MVVM), componentes reutilizáveis, composição de DI
tests/
  WAVE.UnitTests       # Testes da lógica pura
docs/
  ARQUITETURA.md       # Detalhes de arquitetura, padrões e mapeamento da spec
```

Detalhes de arquitetura, design patterns e o mapeamento spec → código estão em [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md).

## Segurança

- **RBAC**: papéis Operador e Administrador; permissões validadas na camada de Aplicação (não só na UI).
- **Credenciais Wi-Fi** cifradas com **DPAPI** (escopo do usuário); nunca em texto claro.
- **Senha administrativa** guardada como hash **PBKDF2-SHA256** com sal aleatório.
- **Validação de entrada** de SSID/URL/host e sanitização de argumentos de linha de comando.
- Dados locais em `%LOCALAPPDATA%\WAVE` (perfis, histórico, credenciais cifradas, logs).

## Limitações conhecidas / próximos passos

- **Redes Enterprise (802.1X)** ainda não são suportadas pelo gerador de perfil (Personal e Open sim).
- **Velocidade (fast.com)** e **streaming (YouTube)** rodam em janelas anônimas do navegador; a captura automática desses números no app é um próximo passo (hoje o histórico registra a telemetria de ping).
- O encerramento de processos entre execuções é por **nome** (`cmd`, `msedge`, `chrome`), conforme a especificação — pode fechar outras janelas desses processos. A lista é configurável em `TestRunnerOptions`.
- A URL do vídeo de teste é neutra e configurável (evita hardcode de exemplo).
