FROM n8nio/n8n:latest

USER root

# Community nodes + Code-node libraries for Helga HR workflow
RUN mkdir -p /home/node/.n8n/nodes \
    && cd /home/node/.n8n/nodes \
    && npm init -y \
    && npm install n8n-nodes-nostrobots@1.2.1 @noble/hashes@1.3.1 @noble/secp256k1@2.1.0 @faker-js/faker@9.3.0 nostr-tools@2.10.4 \
    && chown -R node:node /home/node/.n8n

USER node
