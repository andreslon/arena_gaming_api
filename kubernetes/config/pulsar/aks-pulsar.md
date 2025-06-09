# Kubernetes Cloudflare Tunnel and ExternalDNS Setup


## Deploy Pulsar
```powershell
helm repo add apache https://pulsar.apache.org/charts
helm repo update
kubectl create namespace pulsar

kubectl label namespace pulsar app.kubernetes.io/managed-by=Helm --overwrite
kubectl annotate namespace pulsar meta.helm.sh/release-name=pulsar --overwrite
kubectl annotate namespace pulsar meta.helm.sh/release-namespace=pulsar --overwrite
kubectl apply -f https://raw.githubusercontent.com/prometheus-operator/prometheus-operator/main/bundle.yaml

helm install pulsar apache/pulsar -f pulsar-values.yaml -n pulsar
```    
 ## Get Public Ip
```powershell
kubectl get svc -n pulsar
```

## Uninstall in error case
```powershell
helm uninstall pulsar -n pulsar
kubectl delete namespace pulsar --grace-period=0 --force
kubectl get namespaces

``` 
