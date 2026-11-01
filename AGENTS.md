Project-
Assignment Guideline: Parcel Delivery & Courier Management System
Objective
This assignment tests your knowledge of enterprise application development by designing and implementing a fully functional Parcel Delivery & Courier Management System. The application should let customers and merchants send and track parcels, let dispatchers assign deliveries, and let couriers update delivery progress in real time. It should also integrate with external systems, as enterprise applications rarely operate in isolation. You will design, implement, secure, containerize, test, and deploy a modern enterprise application.
Weightage: 20 marks Group Size: 10 members
Scenario
SwiftDrop is a growing courier company operating island-wide. It currently manages bookings through phone calls and spreadsheets, and customers have no visibility into where their parcels are. SwiftDrop also wants partner online stores (merchants) to create delivery orders automatically from their own e-commerce platforms. Your team has been hired to build the new platform.
Assignment Overview 1. Customer Functionality
● Secure signup and login, including "Login with Google" (OAuth 2.0).
● Create a delivery request (pickup address, drop-off address, parcel size/weight).
● View an estimated delivery fee before confirming.
● Track parcel status in real time (Created → Picked Up → In Transit → Out for Delivery
→ Delivered / Failed).
● View delivery history and download a delivery receipt.
● Mobile-friendly tracking page accessible via a public tracking number. 2. Merchant (Business) Functionality
● Merchant registration with an admin approval step.
● Create delivery orders via a secured API using API keys or OAuth 2.0 client credentials.
● Receive status updates through webhooks when a parcel status changes.
● Dashboard showing all orders and their delivery states. 3. Courier Functionality

● Login and authentication.
● View assigned deliveries for the day.
● Update parcel status at each stage, with a delivery note or proof (e.g., recipient name or
photo upload).
● Mark failed deliveries with a reason. 4. Dispatcher/Admin Functionality
● View all unassigned parcels and assign them to couriers.
● Monitor courier workloads (number of active deliveries per courier).
● Manage users, merchants, and delivery zones/pricing.
● View basic reports (deliveries per day, success/failure rate). 5. System Requirements
● Real-time updates using WebSockets, server-sent events, or polling.
● Role-based access control for Customer, Merchant, Courier, and Dispatcher/Admin.
● JWT-based authentication for the application, plus OAuth 2.0 for social login or merchant
API access.
● At least one asynchronous integration pattern (e.g., publish/subscribe via a message
broker such as RabbitMQ or Kafka) for status-change events, which trigger notifications
and merchant webhooks.
● A notification service (email or a mock SMS service) that consumes status events.
● Containerized deployment (frontend + backend services + database + message broker).
● Secure handling of personal data (addresses, phone numbers): input validation, hashed
passwords, no sensitive data in error messages or logs.
Requirements

1. Design Documentation (20 marks)
   ● Use case diagrams for customer, merchant, courier, and dispatcher interactions.
   ● ER diagram covering users, roles, merchants, parcels, status history, couriers, zones,
   and pricing.
   ● Architecture proposal (e.g., layered, microservices, or modular monolith with a BFF) with
   justification, clearly showing the presentation, business logic, and data layers.
   ● Integration design: identify which integration patterns you used (request/reply,
   publish/subscribe, broker, etc.) and why.
   ● Sequence/activity diagrams for key flows: creating a delivery, merchant API order
   creation, courier status update triggering notifications, and the OAuth 2.0 login flow.
   ● A short note on the design patterns and principles applied (e.g., SOLID, DRY,
   Repository, Strategy for pricing, Observer for events).
2. Implementation (50 marks)

● Frontend: Responsive UI using React, Angular, or Vue.
● Backend: Core logic using a suitable framework (Spring Boot, Node.js/Express, Django,
etc.).
● APIs: Secure REST or GraphQL APIs with proper versioning, validation, and centralized
error handling.
● Integration: Message broker for status events, a notification consumer, and merchant
webhooks with retry on failure.
● Real-time updates: Live parcel tracking via WebSockets/SSE.
● Code quality: Evidence of coding best practices: meaningful exception handling, no
duplicated logic, configuration via environment variables (no hard-coded secrets).
● Containerization:
○ Dockerize all services.
○ Use docker-compose for local orchestration. 3. Testing (15 marks)
● Unit and integration tests for backend services, including at least one test for the event/integration flow.
● Tests for authorization rules (e.g., a courier cannot access another courier's deliveries).
● Measurable code coverage, with test results included in the submission. 4. Presentation/Demo (15 marks)
● Live demo of core features:
○ Customer login (including Google login), delivery creation, and real-time tracking.
○ Merchant creating an order via the API and receiving a webhook.
○ Dispatcher assigning a parcel; courier updating status.
○ Notification triggered by a status change.
● Walkthrough of architecture, diagrams, integration choices, and security decisions.
● Each member should be able to explain the part they contributed.
Bonus Marks
● Kubernetes Deployment (+5 marks): Deploy the full system (frontend, backend services, database, and broker) on Kubernetes using ConfigMaps, Secrets, Service definitions, and Ingress. Helm charts are a plus.
● AI Delivery Assistant (+5 marks): Integrate a Generative AI chatbot that can answer queries such as "Where is my parcel?" or "How much to send 2 kg to Kandy?" by calling your backend APIs. Exposing these capabilities as MCP tools is encouraged.
Submission Requirements
● Source code repository link (with a README containing setup instructions).
● Design documentation (PDF).
● Test reports and coverage summary.

● A contribution sheet listing each member's responsibilities.

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
