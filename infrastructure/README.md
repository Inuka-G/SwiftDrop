# SwiftDrop – Infrastructure (Docker, PostgreSQL, Kubernetes)

## Run everything with Docker Compose
Requires Docker Desktop. From the repository root:

```bash
cp .env.example .env        # optional - defaults work without it
docker compose up --build
```

| Service    | URL                              |
|------------|----------------------------------|
| Frontend   | http://localhost:5173            |
| Swagger    | http://localhost:5017/swagger    |
| RabbitMQ   | http://localhost:15672           |
| PostgreSQL | localhost:5432                   |

Stop: `docker compose down` (add `-v` to delete database data).

### How it fits together
- `infrastructure/backend.Dockerfile` – multi-stage build of `SwiftDrop.Host` (.NET 10).
- `infrastructure/frontend.Dockerfile` – builds the Vite app, serves it with nginx.
- `infrastructure/nginx.conf` – serves the SPA and forwards `/api`, `/hubs` (SignalR) and `/swagger` to the backend, so the frontend should call **relative URLs** such as `/api/v1/...`.
- `infrastructure/postgres/init.sql` – creates one PostgreSQL schema per module.
- The backend runs with `Database__Provider=Postgres`, which overrides the SQLite setting in `appsettings.Development.json`.
- All settings come from environment variables (no secrets in code). Secrets live in `.env`, which is git-ignored.

## Kubernetes (bonus)
Works on Docker Desktop (Settings -> Kubernetes -> Enable), minikube, or kind.

```bash
# 1. build the images the manifests use
docker build -f infrastructure/backend.Dockerfile  -t swiftdrop-backend:latest  .
docker build -f infrastructure/frontend.Dockerfile -t swiftdrop-frontend:latest .
#    minikube only:  minikube image load swiftdrop-backend:latest swiftdrop-frontend:latest
#    kind only:      kind load docker-image swiftdrop-backend:latest swiftdrop-frontend:latest

# 2. install an ingress controller (once)
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.11.2/deploy/static/provider/cloud/deploy.yaml

# 3. deploy
kubectl apply -f k8s/

# 4. watch it start, then open http://localhost
kubectl get pods -n swiftdrop -w
```
(minikube: run `minikube addons enable ingress` instead of step 2, then `minikube tunnel`.)

No ingress? `kubectl port-forward -n swiftdrop svc/frontend 8080:80` and open http://localhost:8080.

Remove everything: `kubectl delete namespace swiftdrop`.
