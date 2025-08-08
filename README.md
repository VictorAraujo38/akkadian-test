# Sistema de Agendamento Médico com Triagem por IA

Sistema completo para agendamento médico com triagem automática de sintomas via IA, desenvolvido com ASP.NET Core 8, React + Next.js e PostgreSQL.

## 🚀 Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8** - Framework web
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **JWT** - Autenticação
- **Swagger** - Documentação da API
- **xUnit** - Testes unitários

### Frontend
- **React 18** - Biblioteca UI
- **Next.js 14** - Framework React
- **TypeScript** - Tipagem estática
- **Tailwind CSS** - Estilização
- **React Hook Form** - Gerenciamento de formulários
- **Axios** - Cliente HTTP

### DevOps
- **Docker** - Containerização
- **Docker Compose** - Orquestração de containers

## 📋 Pré-requisitos

### Opção 1: Com Docker (Recomendado)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado (inclui Docker Compose)

### Opção 2: Sem Docker
- [Node.js 18+](https://nodejs.org/)
- [.NET SDK 8+](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Visual Studio Code](https://code.visualstudio.com/) ou [Visual Studio 2022](https://visualstudio.microsoft.com/)

## 🛠️ Instalação e Execução

### 🐳 Usando Docker (Recomendado)

1. **Clone o repositório**
```bash
git clone https://github.com/seu-usuario/medical-scheduling.git
cd medical-scheduling
```

2. **Configure as variáveis de ambiente**
```bash
cp .env.example .env
# Edite o arquivo .env e adicione sua OpenAI API Key (opcional)
```

3. **Execute com Docker Compose**
```bash
docker-compose up --build
```

4. **Acesse a aplicação**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

5. **Credenciais padrão para teste**

**Médico:**
- Email: medico@example.com
- Senha: Senha@123

**Paciente:**
- Email: paciente@example.com
- Senha: Senha@123

### 💻 Executando Localmente (Sem Docker)

#### Backend

1. **Instale e configure o PostgreSQL**
- Instale o PostgreSQL seguindo as instruções em [postgresql.org](https://www.postgresql.org/download/)
- Crie um banco de dados para o projeto:
```sql
CREATE DATABASE medical_scheduling;
```
- Anote o nome de usuário e senha do PostgreSQL para configurar a connection string

2. **Navegue até a pasta do backend**
```bash
cd backend/MedicalScheduling.API
```

3. **Restaure as dependências**
```bash
dotnet restore
```

4. **Configure a connection string**
Edite `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=medical_scheduling;Username=postgres;Password=postgres"
  }
}
```
**Nota:** Substitua `postgres` pelo seu nome de usuário e senha do PostgreSQL, caso sejam diferentes.

5. **Instale a ferramenta do Entity Framework Core CLI**
```bash
dotnet tool install --global dotnet-ef
```

6. **Execute as migrations**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

7. **Execute o backend**
```bash
dotnet run
```

#### Frontend

1. **Navegue até a pasta do frontend**
```bash
cd frontend/medical-scheduling-web
```

2. **Instale as dependências**
```bash
npm install
```

3. **Configure o arquivo .env.local**
```bash
cp .env.local.example .env.local
# Edite o arquivo .env.local se necessário
```

4. **Execute o frontend**
```bash
npm run dev
```

## 🔧 Configuração da IA

O sistema suporta dois modos de triagem:

### 1. Mock (Padrão)
Usa um sistema de palavras-chave local para simular a IA.

### 2. OpenAI (Opcional)
Para usar a API real da OpenAI:

1. Obtenha uma API Key em https://platform.openai.com/
2. Configure no arquivo `.env` na raiz do projeto (copie de `.env.example` se ainda não existir):
```env
OPENAI_API_KEY=sua-chave-aqui
```

## 📱 Funcionalidades

### Pacientes
- ✅ Cadastro e login
- ✅ Criar agendamentos com sintomas
- ✅ Visualizar histórico de agendamentos
- ✅ Receber recomendação de especialidade via IA

### Médicos
- ✅ Cadastro e login
- ✅ Visualizar agendamentos do dia
- ✅ Filtrar agendamentos por data
- ✅ Ver sintomas e especialidade recomendada

### Sistema
- ✅ Autenticação JWT com roles
- ✅ Triagem automática por IA
- ✅ API RESTful documentada
- ✅ Interface responsiva

## 🧪 Testes

### Backend
```bash
cd backend/MedicalScheduling.API
dotnet test
```

### Frontend
```bash
cd frontend/medical-scheduling-web
npm run test  # Executa testes em modo watch
# ou
npm run test:ci  # Executa testes uma única vez (CI)

## 📝 Endpoints da API

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|--------------|
| POST | `/auth/register` | Registro de usuário | Não |
| POST | `/auth/login` | Login | Não |
| POST | `/paciente/agendamentos` | Criar agendamento | Paciente |
| GET | `/paciente/agendamentos` | Listar agendamentos | Paciente |
| GET | `/medico/agendamentos?data=` | Agendamentos por data | Médico |
| POST | `/mock/triagem` | Simular triagem IA | Não |

## 🏗️ Arquitetura

### Backend - Clean Architecture
```
backend/
├── Controllers/      # Endpoints da API
├── Services/        # Lógica de negócio
├── Models/          # Entidades do domínio
├── DTOs/            # Data Transfer Objects
├── Data/            # Contexto do EF Core
└── Migrations/      # Migrations do banco
```

### Frontend - Next.js App Structure
```
frontend/
├── pages/           # Rotas da aplicação
├── components/      # Componentes reutilizáveis
├── contexts/        # Context API (Auth)
├── services/        # Serviços de API
└── styles/          # Estilos globais
```

## 🚀 Deploy

### Heroku
```bash
heroku create medical-scheduling-api
heroku addons:create heroku-postgresql:hobby-dev
git push heroku main
```

### Vercel (Frontend)
```bash
vercel --prod
```

## 🔒 Segurança

- Senhas hasheadas com SHA256
- Tokens JWT com expiração de 7 dias
- CORS configurado para ambiente de produção
- Variáveis sensíveis em variáveis de ambiente
- Validação de entrada em todos os endpoints

## 📈 Melhorias Futuras

Com mais tempo, implementaria:

1. **Técnicas**
   - Redis para cache
   - RabbitMQ para filas
   - GraphQL ao invés de REST
   - Microserviços

2. **Funcionalidades**
   - Notificações por email
   - Chat médico-paciente
   - Upload de exames
   - Dashboard com métricas
   - Integração com calendário

3. **DevOps**
   - CI/CD com GitHub Actions
   - Kubernetes para orquestração
   - Monitoring com Prometheus
   - Logs centralizados com ELK

## 👨‍💻 Autor

Victor Araujo
