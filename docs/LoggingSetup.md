# Add to docker-compose.yml for log aggregation

  loki:
    image: grafana/loki:latest
    container_name: fintechplatform-loki
    ports:
      - "3100:3100"
    command: -config.file=/etc/loki/local-config.yaml
    volumes:
      - loki_data:/loki
    restart: unless-stopped

  promtail:
    image: grafana/promtail:latest
    container_name: fintechplatform-promtail
    volumes:
      - /var/log:/var/log
      - /var/run/docker.sock:/var/run/docker.sock
      - ./monitoring/promtail-config.yml:/etc/promtail/config.yml
    command: -config.file=/etc/promtail/config.yml
    depends_on:
      - loki
    restart: unless-stopped

# Add to volumes section:
volumes:
  loki_data:
