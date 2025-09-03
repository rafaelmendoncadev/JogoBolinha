      

          
# PRD - Sistema de Login e Registro de Usuários
## Jogo Bolinha - Autenticação e Perfis de Usuário

---

## 1. Visão Geral do Produto

### 1.1 Objetivo
Implementar um sistema completo de autenticação de usuários no Jogo Bolinha, permitindo que cada jogador tenha sua própria conta, salve seu progresso individual e mantenha estatísticas personalizadas.

### 1.2 Problema a Resolver
Atualmente, o jogo não possui sistema de autenticação, impossibilitando:
- Salvamento individual de progresso
- Ranking personalizado entre usuários
- Histórico de conquistas por jogador
- Continuidade de sessões entre dispositivos

### 1.3 Benefícios Esperados
- **Para Usuários**: Experiência personalizada, progresso salvo, competição saudável
- **Para o Produto**: Maior engajamento, dados de usuário, retenção melhorada

---

## 2. Funcionalidades Principais

### 2.1 Registro de Usuário
**Descrição**: Permitir que novos usuários criem contas no sistema.

**Campos Obrigatórios**:
- Username (único, 3-50 caracteres)
- Email (único, formato válido)
- Senha (mínimo 8 caracteres, com critérios de segurança)
- Confirmação de senha

**Campos Opcionais**:
- Nome completo
- Data de nascimento
- Avatar (seleção de imagens pré-definidas)

**Validações**:
- Username único no sistema
- Email único e formato válido
- Senha forte (letras, números, caracteres especiais)
- Confirmação de senha deve coincidir

### 2.2 Login de Usuário
**Descrição**: Autenticação de usuários existentes.

**Opções de Login**:
- Username + Senha
- Email + Senha

**Funcionalidades Adicionais**:
- "Lembrar-me" (sessão persistente)
- "Esqueci minha senha" (recuperação por email)
- Bloqueio temporário após tentativas falhadas

### 2.3 Perfil do Usuário
**Descrição**: Área pessoal para gerenciar conta e visualizar estatísticas.

**Informações Exibidas**:
- Dados pessoais editáveis
- Estatísticas de jogo (níveis completados, pontuação total, tempo jogado)
- Conquistas desbloqueadas
- Histórico de partidas
- Posição no ranking

**Ações Disponíveis**:
- Editar informações pessoais
- Alterar senha
- Alterar avatar
- Excluir conta

---

## 3. Especificações Técnicas

### 3.1 Modelos de Dados

#### 3.1.1 Extensão do Modelo Player Existente
```csharp
public class Player
{
    // Campos existentes mantidos
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; }
    
    // Novos campos para autenticação
    [Required]
    public string PasswordHash { get; set; }
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
}
```

#### 3.1.2 Novo Modelo UserSession
```csharp
public class UserSession
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string SessionToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? DeviceInfo { get; set; }
    
    public Player Player { get; set; }
}
```

### 3.2 Serviços Necessários

#### 3.2.1 AuthenticationService
```csharp
public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterModel model);
    Task<AuthResult> LoginAsync(LoginModel model);
    Task<bool> LogoutAsync(string sessionToken);
    Task<bool> ValidateSessionAsync(string sessionToken);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ChangePasswordAsync(int playerId, string currentPassword, string newPassword);
}
```

#### 3.2.2 UserService
```csharp
public interface IUserService
{
    Task<Player?> GetPlayerByIdAsync(int id);
    Task<Player?> GetPlayerByUsernameAsync(string username);
    Task<Player?> GetPlayerByEmailAsync(string email);
    Task<bool> UpdatePlayerAsync(Player player);
    Task<bool> DeletePlayerAsync(int id);
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<bool> IsEmailAvailableAsync(string email);
}
```

### 3.3 Controllers

#### 3.3.1 AuthController
- `GET /Auth/Register` - Exibir formulário de registro
- `POST /Auth/Register` - Processar registro
- `GET /Auth/Login` - Exibir formulário de login
- `POST /Auth/Login` - Processar login
- `POST /Auth/Logout` - Realizar logout
- `GET /Auth/ForgotPassword` - Formulário recuperação senha
- `POST /Auth/ForgotPassword` - Processar recuperação
- `GET /Auth/ResetPassword` - Formulário nova senha
- `POST /Auth/ResetPassword` - Processar nova senha

#### 3.3.2 UserController
- `GET /User/Profile` - Exibir perfil do usuário
- `POST /User/Profile` - Atualizar perfil
- `GET /User/ChangePassword` - Formulário alterar senha
- `POST /User/ChangePassword` - Processar alteração
- `POST /User/DeleteAccount` - Excluir conta

### 3.4 Views e Interface

#### 3.4.1 Páginas de Autenticação
- **Login**: Formulário responsivo com validação client-side
- **Registro**: Formulário com validação em tempo real
- **Recuperação de Senha**: Interface simples e clara
- **Redefinição de Senha**: Formulário seguro

#### 3.4.2 Área do Usuário
- **Dashboard**: Visão geral das estatísticas
- **Perfil**: Edição de dados pessoais
- **Configurações**: Preferências e segurança

---

## 4. Integração com Sistema Existente

### 4.1 Modificações no GameController
- Adicionar verificação de autenticação
- Associar GameState ao Player logado
- Salvar progresso automaticamente

### 4.2 Modificações no GameDbContext
- Adicionar DbSet<UserSession>
- Configurar relacionamentos de autenticação
- Atualizar migrations

### 4.3 Middleware de Autenticação
```csharp
public class AuthenticationMiddleware
{
    // Verificar sessão ativa
    // Redirecionar para login se necessário
    // Injetar dados do usuário no contexto
}
```

---

## 5. Segurança

### 5.1 Criptografia
- **Senhas**: Hash com BCrypt (salt automático)
- **Tokens**: JWT ou GUID seguro para sessões
- **Dados Sensíveis**: Criptografia AES para informações críticas

### 5.2 Validações
- **Input Sanitization**: Prevenir XSS e SQL Injection
- **Rate Limiting**: Limitar tentativas de login
- **CSRF Protection**: Tokens anti-falsificação

### 5.3 Políticas de Senha
- Mínimo 8 caracteres
- Pelo menos 1 letra maiúscula
- Pelo menos 1 número
- Pelo menos 1 caractere especial
- Não pode ser igual ao username

---

## 6. Experiência do Usuário

### 6.1 Fluxo de Primeiro Acesso
1. Usuário acessa o jogo
2. Opção de "Jogar como Convidado" ou "Criar Conta"
3. Se criar conta: formulário de registro
4. Confirmação por email (opcional)
5. Redirecionamento para tutorial/primeiro nível

### 6.2 Fluxo de Usuário Existente
1. Usuário acessa o jogo
2. Formulário de login
3. Autenticação automática se "lembrar-me" ativo
4. Redirecionamento para último nível jogado

### 6.3 Gamificação
- **Conquistas de Registro**: "Bem-vindo!" ao criar conta
- **Streak de Login**: Bônus por dias consecutivos
- **Perfil Completo**: Incentivo para preencher dados

---

## 7. Métricas e Analytics

### 7.1 Métricas de Autenticação
- Taxa de conversão registro → primeiro jogo
- Taxa de retenção por período
- Tempo médio de sessão por usuário
- Abandono no formulário de registro

### 7.2 Métricas de Engajamento
- Frequência de login por usuário
- Níveis completados por usuário
- Tempo total jogado por usuário
- Conquistas desbloqueadas

---

## 8. Cronograma de Implementação

### Fase 1 (Semana 1-2): Infraestrutura
- [ ] Criar modelos de autenticação
- [ ] Implementar AuthenticationService
- [ ] Configurar middleware de autenticação
- [ ] Atualizar banco de dados

### Fase 2 (Semana 3-4): Interface
- [ ] Criar páginas de login/registro
- [ ] Implementar AuthController
- [ ] Adicionar validações client-side
- [ ] Testes de interface

### Fase 3 (Semana 5-6): Integração
- [ ] Modificar GameController
- [ ] Implementar área do usuário
- [ ] Conectar salvamento de progresso
- [ ] Testes de integração

### Fase 4 (Semana 7-8): Polimento
- [ ] Implementar recuperação de senha
- [ ] Adicionar funcionalidades de perfil
- [ ] Otimizações de performance
- [ ] Testes finais e deploy

---

## 9. Critérios de Aceitação

### 9.1 Funcionalidades Básicas
- ✅ Usuário pode criar conta com dados válidos
- ✅ Usuário pode fazer login com credenciais corretas
- ✅ Usuário pode recuperar senha por email
- ✅ Progresso do jogo é salvo por usuário
- ✅ Usuário pode visualizar estatísticas pessoais

### 9.2 Segurança
- ✅ Senhas são criptografadas adequadamente
- ✅ Sessões expiram automaticamente
- ✅ Tentativas de login são limitadas
- ✅ Dados sensíveis são protegidos

### 9.3 Performance
- ✅ Login/registro em menos de 2 segundos
- ✅ Interface responsiva em dispositivos móveis
- ✅ Validações em tempo real sem travamentos

---

## 10. Riscos e Mitigações

### 10.1 Riscos Técnicos
- **Migração de Dados**: Testar extensivamente em ambiente de desenvolvimento
- **Performance**: Implementar cache para sessões ativas
- **Segurança**: Auditoria de código e testes de penetração

### 10.2 Riscos de Produto
- **Adoção**: Manter opção "jogar como convidado" temporariamente
- **Complexidade**: Interface simples e intuitiva
- **Abandono**: Processo de registro rápido e opcional

---

Este PRD fornece uma base sólida para implementar o sistema de autenticação no Jogo Bolinha, mantendo a simplicidade do jogo atual enquanto adiciona funcionalidades robustas de usuário.
        