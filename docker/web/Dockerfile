# Stage 1: Build React Vite application
FROM node:22-alpine AS build
WORKDIR /app

# Copy dependency files
COPY web/package.json ./
COPY web/package-lock.json* ./

# Install dependencies
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

# Copy frontend source code and compile
COPY web/ ./
RUN npm run build

# Stage 2: Serve static files with Nginx
FROM nginx:1.27-alpine AS runtime

# Copy custom Nginx configuration
COPY docker/web/nginx.conf /etc/nginx/conf.d/default.conf

# Copy build artifacts to Nginx html directory
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
