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
- Docker Desktop instalado
- Docker Compose

### Opção 2: Sem Docker
- Node.js 18+
- .NET SDK 8+
- PostgreSQL 15+
- Visual Studio Code ou Visual Studio 2022

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

### 💻 Executando Localmente (Sem Docker)

#### Backend

1. **Configure o PostgreSQL**
```sql
CREATE DATABASE medical_scheduling;
```

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
    "DefaultConnection": "Host=localhost;Database=medical_scheduling;Username=seu_usuario;Password=sua_senha"
  }
}
```

5. **Execute as migrations**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

6. **Execute o backend**
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
```env
NEXT_PUBLIC_API_URL=http://localhost:5000
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
2. Configure no arquivo `.env`:
```env
OPENAI_API_KEY=sk-sua-chave-aqui
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
npm run test
```

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
