# 🧪 Arena Gaming API - Service Testing Guide

Este directorio contiene scripts para testear todos los servicios críticos de la Arena Gaming API.

## 🔧 Servicios Testeados

### 1. **PostgreSQL**
- ✅ Conectividad
- ✅ Operaciones CRUD (Create, Read, Update, Delete)
- ✅ Tiempo de respuesta

### 2. **Redis Cache**
- ✅ Conectividad  
- ✅ Operaciones básicas (String, Hash, List, Set)
- ✅ Expiración de keys
- ✅ Tiempo de respuesta

### 3. **Apache Pulsar**
- ✅ Conectividad
- ✅ Producer/Consumer functionality
- ✅ Múltiples topics
- ✅ Message acknowledgment

### 4. **Gemini AI**
- ✅ API connectivity 
- ✅ Tic-tac-toe scenarios
- ✅ AI decision quality
- ✅ Response time

## 🚀 Cómo Usar

### Para Desarrollo Local

```powershell
# Ejecutar todos los tests
.\test-services.ps1

# Cambiar la URL base si es necesario
# Edita $baseUrl en el script
```

### Para Kubernetes

```powershell
# Opción 1: Script automático (recomendado)
.\test-services-k8s.ps1

# Opción 2: Manual
# En terminal 1:
kubectl port-forward -n services service/arenagaming-api 8080:80

# En terminal 2:
# Edita test-services.ps1 y cambia $baseUrl = "http://localhost:8080"
.\test-services.ps1
```

## 📊 Interpretando Resultados

### Estados de Salud
- 🟢 **SUCCESS**: Servicio funcionando perfectamente
- 🟡 **PARTIAL**: Servicio funciona pero con limitaciones
- 🔴 **FAILED**: Servicio no disponible o con errores críticos

### Métricas Importantes
- **Response Time**: Tiempo de respuesta en millisegundos
- **Success Rate**: Porcentaje de tests exitosos (para Gemini AI)
- **Connectivity**: Estado de conectividad básica

## 🐛 Troubleshooting

### Redis Error: "admin mode not enabled"
✅ **Solucionado**: El health check ya no usa comandos administrativos

### Gemini API Key Issues
```powershell
# Verificar que la API key esté configurada
echo $env:Gemini_ApiKey

# En Kubernetes, verificar ConfigMap
kubectl get configmap arenagaming-api -n services -o yaml | grep Gemini
```

### Pulsar Connection Issues
- Verificar que el cluster Pulsar esté corriendo
- Revisar la configuración del namespace y tenant
- Comprobar firewall/network policies

### PostgreSQL Connection Issues
- Verificar connection string
- Comprobar certificados SSL
- Revisar network access rules en Azure

## 📋 Endpoints de Testing

| Servicio | Health Check | Detailed Test |
|----------|-------------|---------------|
| **General** | `GET /api/health` | - |
| **PostgreSQL** | `GET /api/health/postgresql` | `POST /api/health/postgresql/test` |
| **Redis** | `GET /api/health/redis` | `POST /api/health/redis/test` |  
| **Pulsar** | `GET /api/health/pulsar` | `POST /api/health/pulsar/test` |
| **Gemini AI** | - | `POST /api/health/gemini/test` |

## 🔄 CI/CD Integration

Para integrar en pipelines CI/CD:

```yaml
# Azure DevOps / GitHub Actions
- name: Test API Services
  run: |
    pwsh -File test-services.ps1
  env:
    API_BASE_URL: ${{ secrets.API_BASE_URL }}
```

## 📈 Monitoreo Continuo

Los health checks pueden ser usados por:
- **Kubernetes Liveness/Readiness probes**
- **Application Insights**
- **Prometheus/Grafana**
- **Custom monitoring dashboards**

## 🆘 Soporte

Si encuentras problemas:

1. Revisar logs de la aplicación:
   ```bash
   kubectl logs -n services deployment/arenagaming-api
   ```

2. Verificar estado de pods:
   ```bash
   kubectl get pods -n services
   ```

3. Comprobar ConfigMaps y Secrets:
   ```bash
   kubectl get configmap arenagaming-api -n services
   ```

---

## 🎯 Resultados Esperados

Un deployment saludable debería mostrar:
- ✅ 8-9 tests exitosos de 9 totales
- ⏱️ Response times < 2000ms para la mayoría
- 🤖 Gemini AI con success rate > 75% 