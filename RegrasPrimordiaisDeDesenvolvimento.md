# Regras Primordiais para Desenvolvimento

Estas diretrizes são obrigatórias e devem ser seguidas durante todo o ciclo de desenvolvimento.

## 1. Respeite os princípios SOLID

Todo código desenvolvido deve seguir os princípios SOLID, garantindo baixo acoplamento, alta coesão, facilidade de manutenção, extensibilidade e testabilidade.

## 2. Utilize Design Patterns quando apropriado

Aplique Design Patterns sempre que agregarem valor à arquitetura da solução. Evite implementações desnecessárias ("overengineering"), utilizando padrões apenas quando resolverem problemas reais de organização, reutilização ou escalabilidade do código.

## 3. Priorize Clean Code

Todo código deve ser escrito visando legibilidade e manutenção.

Boas práticas incluem:

- Métodos com responsabilidade única.
- Classes coesas.
- Nomes claros e autoexplicativos.
- Evitar duplicação de código (DRY).
- Manter funções pequenas e objetivas.
- Eliminar código morto ou desnecessário.
- Evitar números mágicos e hardcodes quando possível.
- Escrever código que seja facilmente compreendido por outro desenvolvedor.

## 4. Segurança e Permissionamento

Sempre que houver funcionalidades administrativas, painéis de administração ou operações sensíveis, é obrigatório garantir a segurança da aplicação de ponta a ponta.

Isso inclui, no mínimo:

- Controle de acesso baseado em permissões e papéis (RBAC ou equivalente).
- Validação de autorização em todas as camadas da aplicação, não apenas na interface.
- Proteção contra acesso indevido a rotas, serviços e recursos.
- Validação de entrada de dados.
- Tratamento adequado de exceções sem exposição de informações sensíveis.
- Princípio do menor privilégio para usuários e serviços.
- Garantia de que nenhuma operação administrativa possa ser executada sem a devida autorização.

## Objetivo

Todo desenvolvimento deve priorizar:

- Manutenibilidade.
- Escalabilidade.
- Segurança.
- Legibilidade.
- Baixo acoplamento.
- Facilidade de testes.
- Consistência arquitetural.