#  **SupportDesk Pro**

## **About The Project**
SupportDesk Pro is a multi-tenant customer support platform built with .NET 9 and React TypeScript, deployed on Microsoft Azure. I built this for businesses that need a reliable and structured way to manage customer support — a system where tickets are tracked, SLAs are enforced, agents are accountable, and nothing gets lost in an email thread. It also 
integrates Claude AI for ticket categorization, reply drafting, similar ticket search, 
and customer sentiment detection — features that directly reduce agent workload and 
improve response quality.

**Live app**: https://kind-coast-000fe8c1e.2.azurestaticapps.net

**API docs**: https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/swagger

---

### **The Problem It Solves**

Most support operations start with email. It works until it doesn't — tickets get missed, response times are inconsistent, and there is no visibility into who is handling what. I wanted to build something that solves this properly, the way a real company would.
The result is a platform that handles the full ticket lifecycle from creation to closure, enforces SLA deadlines automatically, upload/download supporting documents, sends real email notifications, and keeps every company's data completely isolated even though they share the same database. Multiple companies can run on a single deployment. Each one manages their own team, agents, categories, and SLA policies independently.

Beyond the core platform, the system has a practical AI layer built on top. Agents waste 
time triaging tickets, writing the same replies repeatedly, and walking into conversations 
without knowing the customer is already frustrated. Claude AI addresses each of these 
directly — suggesting category and priority as the customer types, drafting contextual 
replies agents can edit and send, surfacing similar resolved tickets with extracted 
resolution steps, and detecting customer sentiment before the agent writes a single word.

### **How It Works**

There are three user roles, each with a distinct experience.

**Customers** register under their company's tenant using a slug that identifies which 
organisation they belong to. They raise tickets when they need help, communicate with 
the support team through a threaded conversation, upload/download supporting documents, and receive email updates whenever an agent responds or changes the ticket status. They can see SLA countdown timers on their tickets so they always know whether the team is on track. As they type a new ticket, Claude AI reads the title and description and suggests the most appropriate category and priority — the customer can apply the suggestion with one click or ignore it entirely.

**Agents** see only the tickets assigned to them — not the entire queue. They reply 
publicly to customers, download/upload the supporting documents, leave internal notes that customers never see, update ticket status as work progresses, and track live SLA timers that show how much time remains before a deadline is breached. Before writing a reply, agents see a sentiment banner that tells them whether the customer is frustrated, concerned, or calm — along with the exact phrases that triggered it and specific advice on how to respond. They can also click "Draft with AI" to get a Claude-generated reply based on the full ticket history, 
adjusted automatically depending on whether it is a public reply or an internal note. 
A similar tickets panel shows up to three resolved tickets with semantically related 
issues and the actionable steps that fixed them, so agents can apply proven solutions 
without searching manually.

**Administrators** have full visibility across everything. They invite and manage agents, 
configure ticket categories and SLA policies, assign tickets, and monitor team 
performance through the analytics dashboard. Every card on the dashboard is clickable 
and navigates directly to a filtered ticket list. All the AI features available to agents 
are also available to admins when they open a ticket directly. When assigning a ticket, 
the agent dropdown is sorted by current open ticket count — the agent with the least 
active tickets appears at the top with a star, making it easy to distribute workload 
evenly across the team without checking dashboards or asking around.

---

## **Built With**

### **Backend — .NET 9**

- .NET 9 Web API with Swagger UI
- Clean Architecture — Domain / Application / Infrastructure / API layers
- CQRS with MediatR — commands for writes, queries for reads, controllers are thin wrappers
- Entity Framework Core — code-first migrations, global query filters for tenant isolation and soft delete
- SQL Server — Azure SQL Database in production, LocalDB for development
- JWT Authentication — short-lived access tokens (15 min) with refresh token rotation
- BCrypt — password hashing via a custom IPasswordHasher interface
- FluentValidation — request validation runs in a MediatR pipeline behavior before any handler executes
- MailKit — SMTP email via Gmail App Passwords
- Serilog — structured logging with daily rolling files and console sink
- Global Exception Middleware — consistent error responses across all endpoints
- ICurrentTenantService — resolves TenantId, UserId and Role from JWT claims on every request

### **AI — Claude API**

- Anthropic SDK with Claude Haiku (claude-haiku-4-5-20251001)
- Ticket categorization — suggests category and priority as customer types
- AI reply drafting — generates context-aware replies for agents and admins
- RAG-inspired similar ticket search — SQL pre-filters candidates, Claude scores semantically
- Customer sentiment detection — warns agents of frustrated or concerned customers before they reply
- All AI features degrade gracefully — failures return safe defaults, never block the workflow

### **Frontend — React 18 + TypeScript**

- React 18 with TypeScript — type-safe component architecture
- React Router v6 — client-side routing with role-based protected routes
- Zustand — global auth state with localStorage persistence, cache cleared on logout
- Axios — HTTP client with request interceptor for JWT attachment and response interceptor for 401 handling
- TanStack React Query — server state management, data fetching, caching, background refetch, and cache invalidation
- React Hook Form with Zod — form handling with schema validation and cross-field rules
- Tailwind CSS — utility-first styling
- Recharts — dashboard charts and analytics visualizations
- React Hot Toast — toast notification system for user feedback

### **Infrastructure — Azure**

- Docker — multi-stage Dockerfile (SDK for build, ASP.NET runtime for final image)
- Docker Hub — container image registry for storing and pulling API images
- Azure Container Apps — hosts the .NET API, pulls image from Docker Hub on deploy
- Azure Static Web Apps — hosts the React frontend with global CDN distribution
- Azure SQL Database — serverless tier with automatic pause/resume
- Azure Blob Storage — private container for ticket and comment file attachments, accessed via time-limited SAS signed URLs
- GitHub Actions — automated frontend deployment pipeline, triggers on push to master, injects environment variables from repository secrets during build
- Backend deployment is currently manual via Docker CLI and Azure CLI
- UptimeRobot — pings /health endpoint every 5 minutes to prevent Azure SQL auto-pause
- Health Endpoint — GET /health queries the database to confirm connectivity and wake the DB if paused


---

## **Architecture**

```
React TypeScript SPA (Azure Static Web Apps)
        |
        | HTTPS + JWT
        |
.NET 9 Web API (Azure Container Apps)
        |
        |-- MediatR Handlers (CQRS)
        |-- JWT Token Service
        |-- Email Service (MailKit/SMTP)
        |-- Current Tenant Service
        |-- BlobStorageService (file storage)
        |-- AI Services
        |       |-- IAICategorizationService   → suggests category + priority as customer types
        |       |-- IAIDraftReplyService       → context-aware reply drafting for agents
        |       |-- IAISimilarTicketService    → RAG-inspired similar ticket search (SQL pre-filters candidates + Claude scores semantically)
        |       |-- IAISentimentService        → detects customer frustration before agent replies
        |
        |                              |
        ▼                              ▼
Azure SQL Database              Anthropic Claude API
- TenantId on every table       - claude-haiku-4-5-20251001
- EF Core Global Query          - IAICategorizationService
  Filters enforce isolation     - IAIDraftReplyService
- Soft delete on all entities   - IAISimilarTicketService (after SQL pre-filter)
                                - IAISentimentService
```

---

## **Project Structure**

```

SupportDeskPro/
├── SupportDeskPro.API/
│   ├── Controllers/                    -- Thin controllers, no business logic
│   ├── Middleware/                     -- Tenant resolver, global exception handler
│   ├── appsettings.json                -- Base configuration (SMTP, JWT structure)
│   ├── appsettings.Development.json    -- Local overrides (LocalDB, localhost URLs)
│   └── Program.cs                      -- DI registration, middleware pipeline
│
├── SupportDeskPro.Application/
│   ├── Behaviors/                      -- MediatR pipeline behaviors (logging, validation)
│   ├── Features/
│   │   ├── Auth/                       -- Register, Login, VerifyEmail, Forgot and Change Password
│   │   ├── Categories/                 -- Create, Get, Update categories
│   │   ├── Comments/                   -- Create, Get Update and Delete Comment
│   │   ├── Dashboard/                  -- Admin and Agent stats queries
│   │   ├── Notifications/              -- GetNotifications, MarkAsRead, MarkAllAsRead
│   │   ├── SLAPolicies/                -- Full CRUD per priority level
│   │   ├── Tenants/                    -- Create, Get, Update tenants
│   │   ├── Tickets/                    -- Create Ticket, Get Tickets, AssignTicket,UpdateStatus, AIGetSimilarTickets,
                                           AIAnalyseSentiment, AIDraftReply,AISuggest
│   │   └── Users/                      -- InviteAgent, GetUsers, UpdateStatus, UpdateRole, etc
│   ├── Interfaces/
│   │   ├── IAICategorizationService.cs
│   │   ├── IAIDraftReplyService.cs
│   │   ├── IAISentimentService.cs
│   │   ├── IAISimilarTicketService.cs
│   │   ├── IApplicationDbContext.cs
│   │   ├── IBlobStorageService.cs
│   │   ├── ICurrentTenantService.cs
│   │   ├── IEmailService.cs
│   │   ├── IJwtTokenService.cs
│   │   └── IPasswordHasher.cs
│   └── DependencyInjection.cs          -- Application layer service registration
│
├── SupportDeskPro.Domain/
│   ├── Entities/                       -- Ticket, User, Tenant, Category, SLAPolicy,Attachments etc.
│   ├── Enums/                          -- TicketStatus, TicketPriority, UserRole, etc.
│   └── Exceptions/                     -- NotFoundException, BusinessValidationException, etc
│
├── SupportDeskPro.Infrastructure/
│   ├── Migrations/                     -- EF Core migration history
│   ├── Persistence/                    -- ApplicationDbContext, entity configurations
│   └── Services/                       -- EmailService, CurrentTenantService, JwtTokenService,  AI related services etc
│
└── SupportDeskPro.Contracts/
│    └── */                             -- Request and response DTOs per feature
│                            
└── supportdesk-frontend/
    ├── public/                         -- Static assets served directly
    ├── src/
    │   ├── api/
    │   │   ├── axiosClient.ts          -- Axios instance with JWT interceptor and base URL config
    │   │   ├── authApi.ts              -- Login, register, verify email, forgot/reset password calls
    │   │   ├── categoryApi.ts          -- Get active categories for ticket creation dropdown
    │   │   ├── notificationApi.ts      -- Get notifications, mark as read, unread count
    │   │   └── ticketApi.ts            -- Create, get, update tickets and comments
    │   │
    │   ├── components/
    │   │   └── common/
    │   │       ├── Layout.tsx          -- Page wrapper with navbar and content area
    │   │       └── Navbar.tsx          -- Top navigation with role-based links, bell icon, user dropdown
    │   │
    │   ├── pages/
    │   │   ├── admin/
    │   │   │   ├── CategoriesPage.tsx  -- Create, edit, activate/deactivate ticket categories
    │   │   │   ├── DashboardPage.tsx   -- Ticket volume, agent workload, SLA metrics, category 
    │   │   │   ├── SLAPoliciesPage.tsx -- Response and resolution time targets per priority
    │   │   │   ├── TicketsPage.tsx     -- All tenant tickets with filters, search, and assignment
    │   │   │   └── UsersPage.tsx       -- Invite agents, manage roles, activate/deactivate accounts
    │   │   │
    │   │   ├── agent/
    │   │   │   └── AgentDashboardPage.tsx   -- Personal queue with SLA  and per summary    
    │   │   │
    │   │   ├── auth/
    │   │   │   ├── ChangePasswordPage.tsx   -- Change password with verification
    │   │   │   ├── ForgotPasswordPage.tsx   -- Request password reset email
    │   │   │   ├── LoginPage.tsx            -- Email and password login with role-based redirect
    │   │   │   ├── RegisterPage.tsx         -- Customer self-registration with tenant slug
    │   │   │   ├── ResetPasswordPage.tsx    -- Set new password using token from email link
    │   │   │   └── VerifyEmailPage.tsx      -- Auto-verifies email from link, redirects to login
    │   │   │
    │   │   ├── customer/
    │   │   │   ├── CreateTicketPage.tsx        -- Ticket creation form with category and priority,upload/download doc 
    │   │   │   ├── CustomerDashboardPage.tsx   -- Ticket summary cards and recent tickets 
    │   │   │   ├── MyTicketsPage.tsx           -- Customer's own tickets with status filter tabs
    │   │   │   └── TicketDetailPage.tsx        -- Tickets view, conversation, SLA timers, status history,
                                                   Category and  Priority suggestion, Draft with AI, upload/download attachments, 
                                                   Similar past tickets,sentimental suggestion, etc
    │   │   │
    │   │   └── shared/
    │   │       └── NotificationsPage.tsx       -- notifications with mark as read and mark all as read
    │   │
    │   ├── store/
    │   │   └── authStore.ts                    -- Zustand store for JWT token, user info, login and logout actions
    │   │
    │   ├── types/
    │   │   ├── api.types.ts            -- ApiResponse<T> wrapper type matching backend response 
    │   │   ├── auth.types.ts           -- User, login request and response interfaces
    │   │   └── ticket.types.ts         -- Ticket, comment, SLA and status history interfaces
    │   │
    │   ├── App.css                     -- Global styles
    │   ├── App.tsx                     -- Route definitions and protected route wrappers
    │   ├── index.css                   -- Tailwind base imports
    │   └── index.tsx                   -- React app entry point
    │
    ├── .env.local                      -- Local development environment variables (gitignored)
    ├── .env.production                 -- Production environment variables (gitignored)
    ├── .gitignore                      -- Excludes node_modules, build output, env files
    ├── package.json                    -- Dependencies and npm scripts
    ├── postcss.config.js               -- PostCSS configuration for Tailwind
    ├── staticwebapp.config.json        -- Azure Static Web Apps routing config for React Router
    ├── tailwind.config.js              -- Tailwind CSS configuration
    └── tsconfig.json                   -- TypeScript compiler configuration

```
---

## **Key Features**

### **Multi-Tenancy**
Every database table has a TenantId column. EF Core global query filters are applied at the DbContext level so every query is automatically scoped to the current tenant. It is structurally impossible for a query to return data from another tenant — even if a developer forgets to add a filter manually. One database, completely isolated data per company.

### **Authentication**
Registration creates a customer account tied to a tenant by slug — the slug identifies which company the user belongs to. Login validates credentials against a BCrypt hash, generates a signed JWT access token (15 minutes expiry) with a refresh token for rotation, and records the login timestamp. The /me endpoint reads claims directly from the token without touching the database. Email verification is required before login — a tokenised link is sent on registration and expires in 24 hours. Password reset follows the same pattern with a 1-hour expiry. All tokens are stored as SHA-256 hashes — the raw value never persists.

### **Tenant Management**
SuperAdmin manages tenants across the platform. Each tenant has a settings record covering timezone, working hours, auto-close policy, and self-registration toggle. Admins can read and update their own tenant settings. The tenant slug is used during customer registration to identify which company the account belongs to.

### **User Management**
Admins invite agents by email. A temporary password is generated and sent automatically — the agent receives a welcome email with their login credentials and should change their password on first login. Admins can activate or deactivate accounts, change roles between Agent and Customer, and view per-agent ticket workload. All authenticated users can update their own profile and change their password from the navbar — changing the password invalidates all existing refresh tokens and forces re-login on other devices.

### **Categories**
Ticket categories support a parent-child hierarchy with sort ordering so they appear in a logical sequence in dropdowns. Admins manage all categories including activation and deactivation. Deactivating a category hides it from new ticket creation without affecting existing tickets. The active categories endpoint is available to all authenticated roles and powers the dropdown on the ticket creation form.

### **SLA Policies**
Admins define one SLA policy per priority level — Critical, High, Medium, and Low. Each policy sets two targets: first response time and resolution time. Resolution time must always exceed first response time, enforced at both the validator and handler level. When a ticket is created, the system looks up the matching active policy, calculates the deadlines, and stores them on the ticket. The ticket detail page shows live countdown timers that tick every second. When a deadline passes, the ticket is flagged as breached and the admin receives an automatic email alert.

### **Tickets**
Tickets are the core of the system. Creating a ticket generates a sequential tenant-scoped ticket number starting at 1001, assigns the SLA policy, calculates deadlines, and logs the initial Open status to history. Every status change is append-only — each transition is recorded with who changed it, when, and an optional note. Assigning a ticket to an agent auto-transitions it from Open to In Progress and logs the assignment to history. Admins and agents can filter tickets by status, priority, category, assigned agent, SLA breach status, and free-text search across title and description.

### **Ticket Lifecycle**
Open → In Progress → On Hold → Resolved → Closed. Transitions are validated on the backend — you cannot skip states or move backwards except to reopen a resolved ticket. Every transition is logged permanently to the status history table. The timeline is visible on the ticket detail page showing who changed what and when.

### **Comments and Internal Notes**
The ticket conversation thread supports two types of messages. Public replies are visible to everyone on the ticket and trigger email notifications to the other party. Internal notes are only visible to agents and admins — the backend filters them out completely from customer-facing responses and they are displayed with an amber background and a lock icon so agents can distinguish them at a glance. The first public comment from an agent or admin automatically sets the SLA first response timestamp, stopping the first response timer. Comment authors can edit their own comments. Admins can delete any comment.

### **File Attachments**

Sometimes a screenshot explains the problem better than words ever could. Customers can 
attach files when raising a ticket or adding a reply — agents can do the same when 
responding. The file appears below the message it was attached to, with the name, size, 
and a download button right there in the conversation thread.

Files are stored in Azure Blob Storage with the container set to private. This means 
the raw blob URL does nothing — clicking it returns an access denied error. Every 
download goes through the API first, which validates the request and generates a 
time-limited signed URL that expires after 24 hours. The file is only accessible to 
someone who is authenticated and has permission to view that ticket.

A few guardrails are in place on upload. Files larger than 10MB are rejected. Only 
images, PDFs, Word documents, Excel spreadsheets, and plain text are accepted — nothing 
executable. If the file uploads to Blob Storage successfully but the database save fails 
for any reason, the blob is deleted immediately so orphaned files do not accumulate in 
storage.

Ticket-level attachments uploaded during ticket creation appear in the description card. 
Comment-level attachments uploaded with a reply appear below that specific message in 
the conversation thread.


### **AI Features (Claude API)**

This is where the project goes beyond a standard help desk. Four AI features are 
integrated using Claude Haiku — each one targeting a specific pain point in the 
support workflow rather than adding AI for the sake of it.

**1. Ticket Categorization Suggestion**

As a customer types their ticket title and description, Claude reads the content in 
real time and suggests the most appropriate category and priority along with a 
confidence score. The suggestion appears as a banner the customer can apply with one 
click or dismiss entirely. Their manual selection always wins — the AI never overrides 
the customer's choice. If the confidence is below 60% Claude stays silent rather than 
showing an uncertain suggestion.

**2. AI Reply Drafting**

Agents spend a significant portion of their day writing replies that follow the same 
structure — acknowledge the issue, provide steps, offer further help. The "Draft with AI" 
button sends the full ticket history to Claude and gets back a ready-to-edit response. 
What makes this context-aware is the internal note toggle. If the agent is writing a 
public reply to the customer, Claude drafts something friendly and professional with a 
greeting and clear steps. If the toggle is switched to internal note, Claude drafts a 
direct technical note for the team with no pleasantries. The agent always reviews, edits 
if needed, and decides when to send — Claude never sends anything on its own.

**3. RAG-Inspired Similar Ticket Search**

When an agent opens a ticket, a panel in the right column automatically surfaces up to 
three resolved tickets with semantically related issues. The search runs in two stages 
to keep it fast, accurate, and cost-effective.

Stage 1 is SQL — the database pre-filters up to 20 resolved tickets from the same 
category as the current ticket. This narrows the candidates quickly and cheaply before 
any AI is involved, ensuring Claude only ever sees relevant tickets rather than the 
entire history.

Stage 2 is Claude — the 20 candidates are sent to Claude Haiku which reads each one 
and scores it for semantic similarity against the current ticket. Unlike keyword search, 
Claude understands meaning — "cannot login" and "locked out of account" score as similar 
even though they share no words. Only tickets scoring above 0.5 are returned, and the 
top 3 are shown ordered by score.

For each matched ticket, a third Claude call reads the full public conversation and 
extracts the resolution as actionable steps the agent can follow directly — not a 
description of what happened in the past. A ticket that was resolved by asking the 
customer to clear their browser cache comes back as "Try to clear your browser cache and try logging in again" — something the agent can read in two seconds and act on immediately. Agents can copy the resolution directly into the reply box with one click.

**4. Customer Sentiment Detection**

Before an agent writes a single word, Claude has already read the customer's original 
description and all their subsequent replies to determine their emotional state. A red 
banner means the customer sounds frustrated — it shows the exact phrases that triggered 
the detection and tells the agent to acknowledge the frustration immediately and 
prioritise resolution. Amber means the customer sounds confused or uncertain and needs 
extra clarity. Green banner means the customer is calm and a standard professional response 
is fine. 

All AI features fail silently — if Claude is unavailable for any reason, the workflow 
continues without interruption and the agent simply does not see the panel.

### **Email Notifications**
Nine HTML email templates sent via MailKit over SMTP. Every template uses a consistent layout with the SupportDesk Pro header and a clear call-to-action button:
-	Email verification on registration
-	Password reset with secure expiring token
-	Ticket creation confirmation to customer
-	Ticket assignment notification to agent
-	Reply notification — customer when agent replies publicly, agent when customer replies
-	Status change notification to customer
-	SLA breach alert to admin
-	Agent invitation with temporary password
All email links use FrontendUrl from environment variables — localhost in development, the live Azure URL in production. Email failures are caught and logged without failing the original request.

### **Notifications**
The bell icon in the navbar polls the unread count every 30 seconds without requiring a page refresh. Notifications are created automatically when a ticket is assigned, when someone replies, and when a status changes. Users can mark individual notifications as read or clear all at once.

### **Role-Based Dashboards**
Each role lands on a different dashboard after login, showing only what is relevant to them.

The **Admin dashboard** shows tenant-wide ticket volume, agent workload comparison, category breakdown, SLA compliance metrics, and tickets by priority. Every summary card is clickable and navigates to a filtered ticket list.

The **Agent dashboard** shows their personal queue — open tickets, SLA status across their assigned tickets, tickets resolved today, and average resolution time. It is focused entirely on what the agent needs to act on.

The **Customer dashboard** shows a summary of their own tickets — total raised, open, resolved, and any SLA breaches. Quick action buttons let them raise a new ticket or jump straight to their ticket list. The Resolved card navigates directly to a filtered view showing only resolved tickets.


### **Caching**

Every time a customer opens the ticket creation form, the app needs the list of active 
categories. Every time a ticket is created, it needs to look up the SLA policy for that 
priority. Every time an admin opens a ticket to assign it, it needs the full agent list 
with workload counts. These are the same queries running hundreds of times a day 
returning data that almost never changes.

In-memory caching using .NET IMemoryCache eliminates the redundant database hits. 
Active categories and SLA policies are cached for one hour per tenant — they change 
so rarely that stale data is not a real concern, and on the rare occasion an admin 
updates a category or policy, the cache is invalidated immediately so the next request 
fetches fresh data. The agent list is cached for five minutes since workload counts 
shift throughout the day as tickets are assigned and resolved. Dashboard statistics 
follow the same five minute window — fresh enough to be useful, short enough that an 
admin refreshing the page does not hammer the database with expensive aggregation queries.

The similar ticket AI search results are cached for one hour per ticket. Claude API 
calls cost money and take time — if two agents open the same ticket within the hour, 
the second one gets the result instantly from memory rather than triggering a second 
API call.

All cache keys are scoped by tenant ID so one company's cached data can never 
accidentally be served to another tenant. In a multi-server production environment 
the natural next step would be replacing IMemoryCache with Redis for distributed 
caching — the invalidation logic and key structure would stay exactly the same.

---

## **Database – Azure SQL Database**

The database has 18 tables:

```

Authentication & Identity
├── Users                    -- All roles in one table (Customer, Agent, Admin)
├── RefreshTokens            -- JWT refresh token rotation
└── PasswordResetTokens      -- Email verification and password reset tokens

Tenant Management
├── Tenants                  -- Company workspaces, each with isolated data
└── TenantSettings           -- Per-tenant configuration and preferences

Support Configuration
├── Categories               -- Ticket categories with parent/child hierarchy
└── SLAPolicies              -- Response and resolution time targets per priority

Tickets
├── Tickets                  -- Core ticket with status, priority, SLA fields
├── TicketComments           -- Public replies and internal agent notes
├── TicketAttachments        -- File metadata (files stored in Blob Storage)
├── TicketStatusHistory      -- Audit trail of every status change
├── TicketAssignmentHistory  -- Audit trail of every agent assignment
└── TicketNumberSequences    -- Per-tenant sequential ticket numbering (starts at 1001)

Activity & Monitoring
├── Notifications            -- In-app notifications with read tracking
├── EmailLogs                -- Record of every email sent by the system
└── AuditLogs                -- System-wide audit trail

```

Every main table has soft delete (IsDeleted, DeletedAt, DeletedBy) and audit fields (CreatedAt, UpdatedAt) inherited from BaseEntity. Tenant isolation is enforced by a Global Query Filter on TenantId which applies automatically to every EF Core query scoped to a tenant. The only place this filter is bypassed is the login handler, which needs to find a user before a JWT token exists.

---

## **Running Locally**

### *Prerequisites*
-	.NET 9 SDK
-	Node.js 18 or higher
-	SQL Server or LocalDB
-	Visual Studio 2022 or VS Code

### *Backend setup*

1. ***Clone the repository***

        git clone https://github.com/anjusavio/SupportDeskPro.git
        cd SupportDeskPro

2. ***Update the connection string in appsettings.json***

        "ConnectionStrings": {
          "DefaultConnection": "Server=localhost;Database=SupportDeskPro;Trusted_Connection=True;TrustServerCertificate=True;"
        },
          "EmailSettings": {
            "FrontendUrl": "http://localhost:3000"
          },
           "AnthropicSettings": {
            "ApiKey": "sk-ant-api03-your-key-here" 
          },
          "AzureStorage": {
            "ConnectionString": "your-connectionstring",
            "ContainerName": "attachments"
          }


      **Note:**

        - The base appsettings.json has SMTP and JWT configuration. 

        - Update the Gmail app password if you want emails to actually send locally. 

        - Get your Anthroic API key from https://console.anthropic.com.

        - Create Storage Account in Azure Portal, create container and get the connection string.       


3. ***Apply migrations from the API project***

        dotnet ef database update \
          --project SupportDeskPro.Infrastructure \
          --startup-project SupportDeskPro.API

4. ***Run the API***

        cd SupportDeskPro.API
        dotnet run
        The API starts at https://localhost:7230. Swagger UI is available at https://localhost:7230/swagger.

---

## **Deployment**

1.	***Backend — Docker + Azure Container Apps***

    ```
      docker build --no-cache -t supportdeskpro-api .  
      docker tag supportdeskpro-api yourdockerhub/supportdeskpro-api:latest
      docker push yourdockerhub/supportdeskpro-api:latest
    ```

2.	***Azure Container Apps – run in cloud shell***

    ```
      az containerapp update \
        --name supportdeskpro-api \
        --resource-group rg-supportdesk \
        --image yourdockerhub/supportdeskpro-api:latest
    ```

3.	***Frontend — GitHub Actions***

       Pushing to master triggers the workflow automatically. 
       The workflow reads REACT_APP_API_URL from repository secrets and injects it into the React build during compilation.

4.	***Database migrations on Azure SQL***

    ```
      dotnet ef database update \
        --project SupportDeskPro.Infrastructure \
        --startup-project SupportDeskPro.API \
        --connection "your-azure-sql-connection-string"
    ```
5.	***Azure Container App environment variables***

    ```
      ConnectionStrings__DefaultConnection     Azure SQL connection string
      JwtSettings__Secret                      JWT signing key (min 32 characters)
      JwtSettings__Issuer                      SupportDeskPro
      JwtSettings__Audience                    SupportDeskPro
      JwtSettings__AccessTokenExpiryMinutes    15
      JwtSettings__RefreshTokenExpiryDays      7
      EmailSettings__FrontendUrl               https://your-app.azurestaticapps.net
      AllowedOrigins                           https://your-app.azurestaticapps.net
      AnthropicSettings__ApiKey                your Anthropic API key 
      AzureStorage__ConnectionString           your Azure Blob Storage connection string
      AzureStorage__ContainerName              attachments
      ASPNETCORE_ENVIRONMENT                   Production
    ```

6.	***Demo Credentials***

      | *Role*   | *Email*                 | *Password*  |
      |----------|-------------------------|-------------|
      | Admin    | anjoos.savio@gmail.com  | Admin@123   |
      | Agent    | anju.savio90@gmail.com  | Anoop@1234  |
      | Customer | testAndrea@gmail.com    | Andrea@1234 |

The Azure SQL free tier pauses after inactivity. The first request after a quiet period may take 30 to 60 seconds while the database wakes up. UptimeRobot pings the /health endpoint every 5 minutes to keep it alive.

Check the Health of App : https://supportdeskpro-api.victoriousdune-73ebad30.westus.azurecontainerapps.io/health


---

## **API Endpoints**

## Auth
- **POST** `/api/auth/register` — Register a new user
- **POST** `/api/auth/login` — Login with credentials
- **GET** `/api/auth/me` — Get current authenticated user
- **POST** `/api/auth/verify-email` — Verify user email address
- **POST** `/api/auth/forgot-password` — Send password reset email
- **POST** `/api/auth/reset-password` — Reset password using token
- **POST** `/api/auth/change-password` — Change current user password

## Categories
- **GET** `/api/categories` — Get all categories
- **POST** `/api/categories` — Create a new category
- **GET** `/api/categories/{id}` — Get a category by ID
- **PUT** `/api/categories/{id}` — Update a category by ID
- **PATCH** `/api/categories/{id}/status` — Update category status
- **GET** `/api/categories/active` — Get all active categories

## Comments
- **GET** `/api/tickets/{ticketId}/comments` — Get all comments for a ticket
- **POST** `/api/tickets/{ticketId}/comments` — Add a comment to a ticket
- **PUT** `/api/tickets/{ticketId}/comments/{commentId}` — Update a comment by ID
- **DELETE** `/api/tickets/{ticketId}/comments/{commentId}` — Delete a comment by ID

## Dashboard 
- **GET** `/api/dashboard/admin` — Get admin dashboard data 
- **GET** `/api/dashboard/agent` — Get agent dashboard data

## Health
- **GET** `/health` — Check server health status

## Notifications
- **GET** `/api/notifications` — Get all notifications
- **GET** `/api/notifications/unread-count` — Get unread notifications count
- **PATCH** `/api/notifications/{id}/read` — Mark a notification as read
- **PATCH** `/api/notifications/read-all` — Mark all notifications as read

## SLA Policies
- **GET** `/api/sla-policies` — Get all SLA policies
- **POST** `/api/sla-policies` — Create a new SLA policy
- **GET** `/api/sla-policies/{id}` — Get a SLA policy by ID
- **PUT** `/api/sla-policies/{id}` — Update a SLA policy by ID
- **PATCH** `/api/sla-policies/{id}/status` — Update SLA policy status

## Tenants
- **GET** `/api/tenants` — Get all tenants
- **POST** `/api/tenants` — Create a new tenant
- **PATCH** `/api/tenants/{id}/status` — Update tenant status
- **GET** `/api/tenants/my` — Get current tenant
- **GET** `/api/tenants/{id}` — Get a tenant by ID
- **PUT** `/api/tenants/{id}` — Update a tenant by ID
- **PUT** `/api/tenants/my/settings` — Update current tenant settings

## Tickets
- **GET** `/api/tickets` — Get all tickets
- **POST** `/api/tickets` — Create a new ticket
- **GET** `/api/tickets/my` — Get current user's tickets
- **GET** `/api/tickets/{id}` — Get a ticket by ID
- **PUT** `/api/tickets/{id}` — Update a ticket by ID
- **PATCH** `/api/tickets/{id}/status` — Update ticket status
- **PATCH** `/api/tickets/{id}/assign` — Assign a ticket
- **GET** `/api/tickets/{id}/history` — Get ticket history
- **GET** `/api/tickets/{id}/similar` — AI : RAG-inspired similar resolved tickets with extracted resolution steps
- **GET** `/api/tickets/{id}/sentiment` — AI : Customer sentiment analysis (Frustrated / Concerned / Neutral)
- **POST** `/api/tickets/{id}/ai-draft-reply` — AI : Generate context-aware reply draft for agent or internal note
- **POST** `/api/tickets/ai-suggest` — AI : Suggest category and priority as customer types
- **POST** `/api/{id}/attachments` — Document upload
- **GET**  `/api/{id}/attachments/{attachmentId}/download` — Document download

## Users
- **GET** `/api/users` — Retrieve a list of all users.
- **POST** `/api/users/invite-agent` — Invite a new agent user to the system.
- **PATCH** `/api/users/{id}/status` — Update the status of a specific user by their ID.
- **PUT** `/api/users/profile` — Replace/update the profile info for the current user.
- **GET** `/api/users/agents` — Retrieve a list of all agent users.
- **GET** `/api/users/agents/workload` — Retrieve current workload summary for agents.
- **GET** `/api/users/{id}` — Retrieve details of a specific user by their ID.
- **PATCH** `/api/users/{id}/role` — Update the role assigned to a specific user by their ID.


---

### **Error Handling**
All errors return RFC 7807 Problem Details:

    
    {
      "type": "https://supportdesk.com/errors/not-found",
      "title": "Resource Not Found",
      "status": 404,
      "detail": "Ticket with identifier 'abc-123' was not found.",
      "instance": "/api/tickets/abc-123",
      "traceId": "a1b2c3d4"
    }
    
The global ExceptionMiddleware wraps the entire request pipeline. Domain exceptions map to specific HTTP status codes. Unexpected exceptions return 500 with a generic message in production and the full stack trace in development only. The traceId in every response correlates directly with server logs.

| *Exception*	                           | *HTTP Status*                | 
|----------------------------------------|------------------------------|
| NotFoundException	                     | 404                          | 
| ConflictException	                     | 409                          | 
| BusinessValidationException	           | 400                          | 
| ForbiddenException	                   | 403                          |
| ValidationException (FluentValidation) | 	400 with field-level errors.| 

---

### **MediatR Pipeline**
Every request passes through two behaviors before reaching the handler.

**LoggingBehavior** runs first. It generates a short correlation ID, logs the request start, runs the next step, then logs completion time. Any request taking over 500ms triggers a slow request warning automatically.

**ValidationBehavior** runs second. It finds all registered FluentValidation validators for the request type and runs them before the handler executes. If validation fails, it throws a ValidationException which the middleware catches and returns as 400 with field-level errors grouped by property name. If no validator is registered for the request type, it passes through without overhead.

---

## **Logging**
```
Serilog writes to both console and rolling daily log files under /logs. Files are retained for 30 days.
[10:24:01 INF] HTTP POST /api/auth/login responded 200 in 142ms
[10:24:01 INF] [abc12345] START LoginQuery
[10:24:01 INF] [abc12345] END LoginQuery completed in 138ms
[10:24:10 WRN] [def45678] BusinessValidationException on POST /api/auth/login — Invalid email or password.
[10:25:03 WRN] [ghi90123] SLOW REQUEST GetTicketsQuery completed in 612ms — consider optimization
Expected business exceptions like validation failures and not-found errors log as Warning. Unhandled exceptions log as Error with the full stack trace
```

---

### **Frontend Notes**
React Query handles all data fetching. Cache invalidation is handled explicitly after mutations. After creating a ticket, invalidateQueries({ queryKey: ['myTickets'] }) ensures the list page shows fresh data when the user navigates back rather than serving stale cache.
JWT tokens are attached to every outgoing Axios request via a request interceptor that reads from localStorage. A response interceptor catches 401 responses, clears auth state from Zustand and localStorage, and redirects to login.
The Zustand auth store persists to localStorage using the persist middleware. Users stay logged in after a browser refresh without making an additional API call on load.
Protected routes check both authentication and role. A customer trying to access an admin route gets redirected to their own home, not a generic error page.

---

### **Ticket Numbering**
Each tenant gets its own sequential ticket number stored in TicketNumberSequences. The first ticket for any tenant starts at 1001. The counter increments on every ticket creation and is completely scoped to the tenant, so two different companies will each have their own #1001, #1002, and so on.

---

### **SLA Tracking**
When a ticket is created, the system finds the active SLA policy matching the ticket's priority and calculates two deadline timestamps stored directly on the ticket: SLAFirstResponseDueAt and SLAResolutionDueAt. When an agent posts the first public comment on a ticket, FirstResponseAt is recorded automatically.
The IsSLABreached flag marks tickets where deadlines have been missed. The background job for automatic breach detection is planned but not yet implemented. For testing, breach status can be set manually via a direct database update.

---

## **What Is Not Built Yet**
The core platform is complete and production-ready. 
A few things still on the list:
-	Reports page with PDF and Excel export
-	Background job for automatic SLA breach detection
-	Customer satisfaction ratings after resolution

---

## **Author**

**Anju Savio**

Senior Full Stack Developer — .NET + React + Azure + Generative AI

*LinkedIn*: https://www.linkedin.com/in/anjusavio

*Email*: anjoos.savio@gmail.com

*GitHub*: https://github.com/anjusavio/SupportDeskPro


---
