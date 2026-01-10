# MoneyBee - Monitoring Stack 📊

Prometheus + Grafana ile MoneyBee servislerini izlemek için hazır monitoring stack.

## Hızlı Başlangıç

### 1. Monitoring Stack'i Başlat

```bash
# Prometheus ve Grafana'yı başlat
docker-compose -f docker-compose.monitoring.yml up -d

# Logları takip et
docker-compose -f docker-compose.monitoring.yml logs -f
```

### 2. Servisleri Başlat

```bash
# Terminal 1 - Auth Service
cd src/Services/MoneyBee.Auth.Service
dotnet run

# Terminal 2 - Customer Service
cd src/Services/MoneyBee.Customer.Service
dotnet run

# Terminal 3 - Transfer Service
cd src/Services/MoneyBee.Transfer.Service
dotnet run
```

### 3. Arayüzlere Erişim

| Servis | URL | Kullanıcı | Şifre |
|--------|-----|-----------|-------|
| **Grafana** | http://localhost:3000 | `admin` | `admin` |
| **Prometheus** | http://localhost:9090 | - | - |
| Auth Metrics | http://localhost:5001/metrics | - | - |
| Customer Metrics | http://localhost:5002/metrics | - | - |
| Transfer Metrics | http://localhost:5003/metrics | - | - |

## Dashboard'lar

Stack başlatıldığında otomatik olarak 3 dashboard yüklenir:

### 1. 📈 System Overview
- **URL**: http://localhost:3000/d/system-overview
- **İçerik**:
  - Tüm servislerin request rate'leri
  - P95 latency karşılaştırması
  - Cache hit rate'leri
  - Error rate'leri (5xx)
  - Active customer/transfer sayıları
  - Total transfer volume

### 2. 👤 Customer Service Dashboard
- **URL**: http://localhost:3000/d/customer-service
- **İçerik**:
  - Cache hit rate (gauge ve timeline)
  - Customer CRUD operation rates
  - Operation latency (P50/P95/P99)
  - Active customer count
  - Total customers created
  - KYC verification success rate

### 3. 💸 Transfer Service Dashboard
- **URL**: http://localhost:3000/d/transfer-service
- **İçerik**:
  - Transfer operation rates (created/completed/failed/cancelled)
  - Transfer success rate (gauge)
  - Transfer volume rate (TRY/sec)
  - Transfer amount distribution (P50/P95/P99)
  - Active transfers
  - Total transfer volume
  - Cache performance

## Prometheus Queries

### Cache Performance

```promql
# Cache Hit Rate (%)
(rate(customer_cache_hits_total[5m]) 
/ 
(rate(customer_cache_hits_total[5m]) + rate(customer_cache_misses_total[5m]))) * 100

# Cache Operations per Second
rate(customer_cache_hits_total[5m]) + rate(customer_cache_misses_total[5m])
```

### Customer Service

```promql
# Customer Creation Rate
rate(customer_created_total[5m])

# KYC Success Rate (%)
(rate(customer_kyc_verification_total{result="verified"}[5m]) 
/ 
rate(customer_kyc_verification_total[5m])) * 100

# P95 Operation Latency
histogram_quantile(0.95, rate(customer_operation_duration_bucket[5m]))

# Active Customers
customer_active_count
```

### Transfer Service

```promql
# Transfer Success Rate (%)
(rate(transfer_completed_total[5m]) 
/ 
rate(transfer_created_total[5m])) * 100

# Transfer Volume (TRY/sec)
rate(transfer_amount_sum[5m])

# P95 Transfer Amount
histogram_quantile(0.95, rate(transfer_amount_bucket[5m]))

# Failed Transfer Rate
rate(transfer_failed_total[5m])
```

### HTTP Metrics

```promql
# Request Rate
sum(rate(http_server_request_duration_count[5m])) by (service)

# Error Rate (5xx)
sum(rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]))
/
sum(rate(http_server_request_duration_count[5m]))

# P95 Latency
histogram_quantile(0.95, 
  sum(rate(http_server_request_duration_bucket[5m])) by (le, service))
```

## Alert Rules (Örnek)

`monitoring/prometheus/alerts.yml` oluşturun:

```yaml
groups:
  - name: moneybee_alerts
    interval: 30s
    rules:
      # Low Cache Hit Rate
      - alert: LowCacheHitRate
        expr: |
          (rate(customer_cache_hits_total[5m]) 
          / 
          (rate(customer_cache_hits_total[5m]) + rate(customer_cache_misses_total[5m]))) < 0.70
        for: 5m
        labels:
          severity: warning
          service: customer
        annotations:
          summary: "Low cache hit rate for Customer Service"
          description: "Cache hit rate is {{ $value | humanizePercentage }} (threshold: 70%)"

      # High Error Rate
      - alert: HighErrorRate
        expr: |
          sum(rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]))
          /
          sum(rate(http_server_request_duration_count[5m])) > 0.05
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value | humanizePercentage }} (threshold: 5%)"

      # High Latency
      - alert: HighLatency
        expr: |
          histogram_quantile(0.95, 
            rate(customer_operation_duration_bucket[5m])) > 500
        for: 5m
        labels:
          severity: warning
          service: customer
        annotations:
          summary: "High operation latency"
          description: "P95 latency is {{ $value }}ms (threshold: 500ms)"

      # Low Transfer Success Rate
      - alert: LowTransferSuccessRate
        expr: |
          (rate(transfer_completed_total[5m]) 
          / 
          rate(transfer_created_total[5m])) < 0.90
        for: 5m
        labels:
          severity: warning
          service: transfer
        annotations:
          summary: "Low transfer success rate"
          description: "Success rate is {{ $value | humanizePercentage }} (threshold: 90%)"
```

Alert'leri aktif etmek için `prometheus.yml`'e ekleyin:

```yaml
rule_files:
  - 'alerts.yml'
```

## Grafana Kullanımı

### Dashboard'a Panel Ekleme

1. Dashboard'u aç
2. "Add panel" → "Add a new panel"
3. Query editör'de Prometheus query yaz
4. Visualization tipini seç (Time series, Gauge, Stat, etc.)
5. "Apply" tıkla

### Yeni Dashboard Oluşturma

1. http://localhost:3000
2. "+" → "Dashboard"
3. "Add a new panel"
4. Query ve visualization ayarla
5. "Save dashboard"

### Alert Oluşturma

1. Panel ayarlarına gir
2. "Alert" tab'ına tıkla
3. "Create alert rule"
4. Condition ve threshold belirle
5. Notification channel ekle

## Docker Compose Komutları

```bash
# Başlat
docker-compose -f docker-compose.monitoring.yml up -d

# Durdur
docker-compose -f docker-compose.monitoring.yml stop

# Logları gör
docker-compose -f docker-compose.monitoring.yml logs -f prometheus
docker-compose -f docker-compose.monitoring.yml logs -f grafana

# Yeniden başlat
docker-compose -f docker-compose.monitoring.yml restart

# Kaldır (data korunur)
docker-compose -f docker-compose.monitoring.yml down

# Kaldır (data silinir)
docker-compose -f docker-compose.monitoring.yml down -v

# Health check
docker-compose -f docker-compose.monitoring.yml ps
```

## Troubleshooting

### Prometheus Servislerimizi Görmüyor

1. Prometheus targets sayfasını kontrol et: http://localhost:9090/targets
2. Target'ların "UP" durumda olduğunu kontrol et
3. Servisler çalışıyor mu kontrol et:
   ```bash
   lsof -i :5001 :5002 :5003
   ```
4. Metrics endpoint'leri erişilebilir mi:
   ```bash
   curl http://localhost:5002/metrics
   ```

### Grafana Dashboard'lar Yüklenmiyor

1. Dashboard provisioning dosyalarını kontrol et:
   ```bash
   ls -la monitoring/grafana/provisioning/dashboards/
   ls -la monitoring/grafana/dashboards/
   ```
2. Grafana loglarını kontrol et:
   ```bash
   docker-compose -f docker-compose.monitoring.yml logs grafana
   ```

### Data Gösterilmiyor

1. Prometheus'ta query çalıştır: http://localhost:9090/graph
2. Metrics var mı kontrol et
3. Time range'i ayarla (son 15 dakika)
4. Servisler yeterince çalıştı mı? (En az 1-2 dakika veri gerekli)

### Container Başlatılamıyor

```bash
# Container'ları kontrol et
docker ps -a

# Logları gör
docker logs moneybee-prometheus
docker logs moneybee-grafana

# Volume'ları kontrol et
docker volume ls | grep moneybee

# Yeniden başlat
docker-compose -f docker-compose.monitoring.yml restart
```

## Data Retention

### Prometheus
- **Default**: 15 gün
- **Değiştirmek için**: `prometheus.yml`'de `--storage.tsdb.retention.time` parametresini ayarla

```yaml
command:
  - '--storage.tsdb.retention.time=30d'
  - '--storage.tsdb.retention.size=10GB'
```

### Grafana
- Dashboard'lar database'de saklanır
- Volume ile persist edilir: `grafana-data`
- Backup almak için:
  ```bash
  docker cp moneybee-grafana:/var/lib/grafana ./grafana-backup
  ```

## Production Önerileri

### 1. Security
- Grafana admin şifresini değiştir
- Prometheus'u authentication arkasına al
- HTTPS kullan (Nginx reverse proxy)

### 2. Scalability
- Prometheus federation kullan
- Thanos/Cortex gibi long-term storage
- Load balancer ile multiple Prometheus instances

### 3. High Availability
- Prometheus replica'ları
- Grafana yedekleme
- Volume backup stratejisi

### 4. Alerting
- Alertmanager ekle
- Slack/PagerDuty integration
- On-call rotation

### 5. Monitoring
- Prometheus'u monitor et (meta-monitoring)
- Disk usage alerts
- Query performance tracking

## Kaynaklar

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [OpenTelemetry Metrics](https://opentelemetry.io/docs/instrumentation/net/)
- [PromQL Cheat Sheet](https://promlabs.com/promql-cheat-sheet/)

## Yardım

Sorun yaşıyorsanız:
1. Logları kontrol edin
2. Health check yapın
3. Documentation'ı okuyun
4. Issue açın veya sorularınızı sorun
