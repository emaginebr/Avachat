FROM wppconnect/server-cli:latest

WORKDIR /usr/src/wpp-server

RUN npm update @wppconnect-team/wppconnect
