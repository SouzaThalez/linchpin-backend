# linchpin-backend

API do LinchPin em **ASP.NET Core (.NET 10)** + **Entity Framework Core** + **MySQL**.
Substitui o json-server e o Firebase usados pelo `LinchpinFront`.

## Estrutura

```
Controllers/     Endpoints HTTP (apenas recebem a requisição e chamam os serviços)
Services/        Regras de negócio (validações, permissões, sincronização de listas)
Models/          Entidades do banco (tabelas)
DTOs/            Formatos de entrada/saída da API (o que o front envia e recebe)
Data/            AppDbContext, Migrations e importação inicial (Seed/db.json)
Infrastructure/  JWT, usuário logado, tratamento de erros
```

## Rodando localmente

1. MySQL rodando em `localhost:3306` (o banco `linchpin` é criado automaticamente).
2. Guarde a senha do MySQL fora do código, com *user-secrets* (rode dentro desta pasta):

   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=linchpin_db;User=root;Password=SUA_SENHA;"
   ```

3. Suba a API:

   ```bash
   dotnet run --launch-profile http
   ```

   A API sobe em `http://localhost:5080`. Na primeira execução:
   - as migrations criam as tabelas;
   - se não houver usuários, os dados de `Data/Seed/db.json` (o antigo banco do json-server) são importados.
     As senhas são gravadas com hash BCrypt e os ids passam a ser gerados pelo MySQL.

4. No front: `npm start` (ele chama `http://localhost:5080/api`, configurado em `src/environments/environment.ts`).

O login é feito em `POST /api/auth/login` e devolve um token JWT. Todas as outras rotas exigem o token
(header `Authorization: Bearer ...`), que o front envia automaticamente pelo `AuthInterceptor`.

## Endpoints

| Recurso | Rota | Observações |
|---|---|---|
| Login | `POST /api/auth/login`, `GET /api/auth/me` | |
| Usuários | `/api/users` (`?role=analista`) | Criar/remover: só administrador. Editar: administrador ou o próprio usuário |
| Simuladores média/alta | `/api/simulators` (`?category=alta` ou `media`) | PUT substitui o simulador inteiro, inclusive os códigos |
| Simuladores baixa | `/api/simulator-classes` | Cada classe contém seus simuladores |
| Cursos | `/api/courses` | |
| Treinamentos | `/api/trainings`, `/api/trainings/{id}/lessons/{lessonId}` | |
| Disciplinas | `/api/disciplines`, `/api/disciplines/{id}/lessons/{lessonId}` | |
| Relatórios | `/api/lesson-reports`, `/api/scenario-reports`, `/api/cleaning-reports`, `/api/maintenance-reports` | Filtros por query string, ex.: `?hasDescription=true&user.name=Thales` |

Cadastros de simuladores, cursos, treinamentos e disciplinas: somente **administrador** e **analista**.

### Sobre os relatórios

Os formulários de relatório ainda estão mudando (ex.: o registro de limpeza por OS), então cada relatório é
salvo como um documento JSON (coluna `Data`, tipo `json` do MySQL). Data, autor e perfil do autor também
são gravados em colunas próprias, indexadas. Quando um formulário estabilizar, ele pode virar tabela própria
sem mudar as rotas.

## Migrations

```bash
dotnet ef migrations add NomeDaMudanca -o Data/Migrations
dotnet ef database update   # ou apenas rode a API: ela aplica as migrations ao iniciar
```

## Produção (servidor online)

Configure por variáveis de ambiente (nunca no `appsettings.json`):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Server=...;Database=linchpin_db;User=linchpin;Password=...;
Jwt__Key=<texto aleatório com 32+ caracteres>
Cors__AllowedOrigins__0=https://endereco-do-front
```

Recomendado: criar um usuário MySQL próprio para a aplicação (não usar `root`).
No front, ajuste `apiUrl` em `src/environments/environment.prod.ts`.
