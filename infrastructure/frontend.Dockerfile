# Build context = repository root (see docker-compose.yml)
# ---------- build stage ----------
FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---------- runtime stage ----------
FROM nginx:1.27-alpine AS runtime
COPY infrastructure/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
