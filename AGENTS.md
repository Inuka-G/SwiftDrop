SwiftDrop: Architecture Proposal
SwiftDrop is built as a modular monolith. The backend (ASP.NET Core) is one deployable application split into ten business modules. The frontend is a React single-page application.
PostgreSQL stores data (one schema per module) and RabbitMQ carries events between modules.

One schema for each modules
Cross-module calls go through Contracts(public interfaces) or RabbitMQ events only.
Each module responsible for its own data(data isolation current approach one schema per module)
Querying data directly from other modules not allowed
Only Contracts(public interface) is public; everything else is internal.

Modules
Module
Boundary context
Owns
Identity
Signup, login, JWT, Google OAuth, roles
Users, Roles, ExternalLogins, RefreshTokens
Merchants
Registration, admin approval, API keys, OAuth clients
Merchants, ApiKeys, ClientCredentials
Pricing
Zones and fee calculation
Zones, PricingRules
Parcels
Booking, tracking number, lifecycle, history, proof of delivery
Parcels, StatusHistory, Proofs, FailureReports
Dispatch
Assigning parcels to couriers, workload
CourierProfiles, Assignments
Webhooks
Merchant callbacks with retry
WebhookEndpoints, WebhookDeliveries
Notifications
Email / mock SMS
NotificationLog
Realtime
Live push to browsers
None (stateless)
Reporting
Deliveries per day, success/failure rate
Daily statistics
AIAssistant
Chatbot and MCP tools
Chat sessions (optional)

Supporting projects:
Host: the only runnable project. It registers every module, configures authentication, Swagger, CORS, and the central exception middleware.
SharedKernel: small technical library (base entity, Result type, integration event base class, messaging abstractions, role constants). It contains no business concepts.
Logical separation
Every module uses the same three-tier structure, implemented as separate projects

Presentation - React FrontEnd +SwiftDrop.<MODULE>.Api (contains Controllers)
Business - SwiftDrop.<MODULE>.Business (contains Services)
Data - SwiftDrop.<MODULE>.Data (contains dbContext , repository, entities)

1. Architecture Overview
   SwiftDrop is built as a Modular Monolith. The backend (ASP.NET Core) is one deployable application split into ten business modules. The frontend is a React Single Page Application (SPA).
   PostgreSQL stores data using one schema per module, while all modules share the same database. RabbitMQ carries events between modules.
   The architecture follows these principles:
   Each module has its own database schema.
   Cross-module calls go through Contracts (public interfaces) or RabbitMQ events only.
   Each module is responsible for its own data, ensuring data isolation through separate schemas.
   Direct querying of data belonging to other modules is not allowed.
   Only Contracts (public interfaces) are public; everything else remains internal.
2. Functional Modules
   Module
   Boundary Context
   Owns
   Identity
   Signup, login, JWT, Google OAuth, roles
   Users, Roles, ExternalLogins, RefreshTokens
   Merchants
   Registration, admin approval, API keys, OAuth clients
   Merchants, ApiKeys, ClientCredentials
   Pricing
   Zones and fee calculation
   Zones, PricingRules
   Parcels
   Booking, tracking number, lifecycle, history, proof of delivery
   Parcels, StatusHistory, Proofs, FailureReports
   Dispatch
   Assigning parcels to couriers, workload
   CourierProfiles, Assignments
   Webhooks
   Merchant callbacks with retry
   WebhookEndpoints, WebhookDeliveries
   Notifications
   Email / mock SMS
   NotificationLog
   Realtime
   Live push to browsers
   None (stateless)
   Reporting
   Deliveries per day, success/failure rate
   Daily statistics
   AIAssistant
   Chatbot and MCP tools
   Chat sessions (optional)

3. Supporting Projects
   Host: The only runnable project. It registers every module and configures authentication, Swagger, CORS, and the central exception middleware.
   SharedKernel: A small technical library containing base entities, Result types, integration event base classes, messaging abstractions, and role constants. It contains no business concepts.
4. Logical Separation
   Every module uses the same three-tier structure, implemented as separate projects.
   Layer
   Project Structure
   Contains
   Presentation
   React Frontend + SwiftDrop.<MODULE>.Api
   Controllers
   Business
   SwiftDrop.<MODULE>.Business
   Services
   Data
   SwiftDrop.<MODULE>.Data
   DbContext, repositories, entities

Presentation Layer
The React frontend provides interfaces for Customers, Merchants, Couriers, and Administrators. Each module's API project contains controllers that handle incoming requests and expose the required endpoints.
Business Logic Layer
The Business projects contain the services and core business logic of their respective modules. They handle operations according to each module's responsibilities.
Data Access Layer
The Data projects contain the DbContext, repositories, and entities required to manage each module's data in its assigned PostgreSQL schema. 5. Module Communication
Modules communicate through two mechanisms:
Contracts (Public Interfaces): Used for synchronous communication between modules. Only the public interfaces are exposed; internal implementations remain hidden.
RabbitMQ Events: Used for asynchronous communication between modules. Events allow other modules to respond to relevant operations without directly accessing the publishing module's internal implementation.
Direct querying of another module's database tables is not permitted. 6. Database Organization
All ten modules share a single PostgreSQL database, with a separate schema assigned to each module. Each module owns and manages its own tables and data operations.
This approach provides logical data separation while allowing the application to use a shared database. 7. Architecture Justification
The Modular Monolithic Architecture was selected because it provides clear separation of business responsibilities while maintaining a single deployable backend application.
It allows team members to work independently on different modules, improves maintainability through defined module boundaries, and simplifies development, testing, and deployment. Compared with microservices, it reduces infrastructure and deployment complexity while supporting future extensions.

SwiftDrop: Integration design

Merchant e com -> (req/res) SwiftDrop

Why - The caller needs an immediate answer synchronously

Courier ->when a courier publish status event ?
Notification module Sends customer an email or sms
Webhooks module notifies the merchants about the status change
Realtime module uses signalR to update customer tracking page

Why - One status change triggers several independent reactions

Message Broker (RabbitMQ) -?

SwiftDrop -> (webhook callback) Merchant e com

Why - Merchants get push updates without polling

WebSocket via SignalR
Why -Live tracking without polling

(OAuth 2.0 Authorization Code)
Delegates credential handling to Google
Token / key-based auth
Why -The right credential for each client type

Ai agent -> (mcp)

Hari nedaaa??

1. Merchant e-commerce → SwiftDrop (request/reply). The store calls POST request with an API key or OAuth client-credentials token and gets the tracking number and fee in the same response. Why: the caller needs an immediate answer, including validation errors.
2. Courier → status event (publish/subscribe). The courier’s update is a normal request. Parcels validates the transition, saves it with an outbox row in one transaction, and a background publisher sends ParcelStatusChanged to RabbitMQ. Why: one status change triggers several independent reactions, and the courier should not wait for them.
3. Message broker (RabbitMQ).? One topic exchange (swiftdrop.events) with a durable queue per consumer (webhooks, notifications, realtime, reporting, dispatch). Consumers are idempotent, retried on failure, and dead-lettered if they keep failing. Events carry all needed data (event-carried state transfer) but no addresses or phone numbers. Why: durable delivery, and a slow or failing consumer never blocks the others.
4. SwiftDrop → merchant (webhook callback). The Webhooks module sends an HMAC-signed HTTPS POST to the merchant’s endpoint, retrying with exponential backoff and logging each attempt. Why: merchants get push updates without polling.
5. Live tracking (WebSocket via SignalR). The Realtime module consumes events and pushes them to browsers in a group named by tracking number. Why: live tracking without polling.
6. Google login (OAuth 2.0 Authorization Code). Google authenticates the user, Identity exchanges the code server-side, then issues its own JWT. Why: credential handling is delegated to Google, and RBAC works the same for every login method.
7. Token and key-based authentication. JWT for browser users, API key or OAuth client credentials for merchants, and no credential for the public tracking page. Why: the right credential for each client type.
8. AI agent → SwiftDrop (MCP). The assistant sends the chat to an LLM, which calls MCP tools (TrackParcel, EstimateFee) that use the Parcels and Pricing contracts in-process. Why: answers come from live data rather than guesses.
9. Parcels → Pricing (in-process request/reply cross module). IPricingService returns the fee before a parcel is saved. Why: the fee is needed immediately, and in-process calls avoid network failures in a single deployable.
10. Parcels → Merchants (in-process request/reply cross module). IMerchantAccess validates the API key before an order is accepted. Why: the order must be accepted or rejected immediately.
11. Merchant registration and approval. ?Register and approve are synchronous requests; MerchantRegistered and MerchantApproved events trigger admin and merchant emails asynchronously. Why: approval is a human step, and emails should not block it.
12. Parcels → Dispatch (publish/subscribe). Dispatch consumes ParcelCreated to build its unassigned list, so Parcels never needs to know Dispatch exists. Why: loose coupling.
13. Dispatcher assigns a courier (request/reply then publish/subscribe). Dispatch calls IParcelAssignment in-process so success or failure is immediate, then publishes CourierAssigned for Notifications and Realtime. Why: the assignment needs an immediate result, while the reactions are asynchronous.
14. Notifications → Identity (in-process request/reply). IUserDirectory supplies the recipient’s email or phone, since events carry IDs only. Why: keeps personal data out of events.
15. Notifications → email/SMS (adapter). IEmailSender and ISmsSender hide SMTP or mock providers, with retries. Why: providers can be swapped or mocked, and a slow mail server cannot block parcel updates.
16. Reporting ← Parcels events (publish/subscribe). Reporting builds its own daily statistics table from events. Why: heavy report queries stay off the transactional Parcels database.
17. Admin dashboard → modules (request/reply). The admin UI calls Identity, Merchants, Pricing, Dispatch, and Reporting through the single Host API. Why: one entry point, one authentication pipeline, one OpenAPI document.
