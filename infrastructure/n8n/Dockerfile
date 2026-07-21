FROM n8nio/n8n:latest

USER root

# Community nodes + Code-node libraries for Helga HR workflow.
# Packages under ~/.n8n/nodes are for community nodes; NODE_PATH exposes the
# same node_modules to the JS task runner (Code node). Do not put @noble/hashes
# in NODE_FUNCTION_ALLOW_EXTERNAL — require('@noble/hashes') throws on modern
# versions and kills the task runner at startup.
RUN mkdir -p /home/node/.n8n/nodes \
    && cd /home/node/.n8n/nodes \
    && npm init -y \
    && npm install n8n-nodes-nostrobots@1.2.1 @noble/hashes@1.3.1 @noble/secp256k1@2.1.0 @faker-js/faker@9.3.0 nostr-tools@2.10.4 \
    && chown -R node:node /home/node/.n8n

ENV NODE_PATH=/home/node/.n8n/nodes/node_modules

USER node
