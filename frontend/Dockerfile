# syntax=docker/dockerfile:1

# Dev server (Vite) com hot-reload — usado pelo serviço `frontend` do compose
# (target: dev). A SPA de PRODUÇÃO não passa por aqui: ela é buildada no
# Dockerfile da API (estágio `spa`) e servida como wwwroot pela própria API.
FROM node:22-alpine AS dev
WORKDIR /app
EXPOSE 5173
# npm install no boot: o bind mount de ./frontend esconde o node_modules da
# imagem (volume anônimo), então as deps são instaladas no container mesmo.
CMD ["sh", "-c", "npm install && npm run dev -- --host 0.0.0.0"]
