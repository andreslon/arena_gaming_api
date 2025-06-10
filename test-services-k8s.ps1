#!/usr/bin/env pwsh

Write-Host "🚀 Testing Arena Gaming API Services in Kubernetes" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan

# Configuration for Kubernetes
$namespace = "services"
$serviceName = "arenagaming-api"
$localPort = "8080"
$servicePort = "80"

Write-Host "`n🔧 Setting up port forwarding..." -ForegroundColor Yellow
Write-Host "kubectl port-forward -n $namespace service/$serviceName ${localPort}:${servicePort}" -ForegroundColor Gray

# Start port forwarding in background
$portForwardJob = Start-Job -ScriptBlock {
    param($namespace, $serviceName, $localPort, $servicePort)
    kubectl port-forward -n $namespace service/$serviceName "${localPort}:${servicePort}"
} -ArgumentList $namespace, $serviceName, $localPort, $servicePort

# Wait a bit for port forwarding to establish
Start-Sleep -Seconds 3

try {
    # Test if port forwarding is working
    try {
        $testConnection = Invoke-WebRequest -Uri "http://localhost:$localPort/api/health" -Method GET -TimeoutSec 5
        Write-Host "✅ Port forwarding established successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Port forwarding failed to establish" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "`n🔧 Trying to set up port forwarding manually..." -ForegroundColor Yellow
        Write-Host "Run this command in another terminal:" -ForegroundColor Cyan
        Write-Host "kubectl port-forward -n $namespace service/$serviceName ${localPort}:${servicePort}" -ForegroundColor White
        
        Read-Host "`nPress Enter when port forwarding is ready"
    }

    # Update base URL for local port forwarding
    $script:baseUrl = "http://localhost:$localPort"
    
    Write-Host "`n📊 Starting Health Checks via Kubernetes..." -ForegroundColor Magenta
    Write-Host "Base URL: $script:baseUrl" -ForegroundColor Gray

    # Source the main testing script functions
    . .\test-services.ps1
    
    Write-Host "`n🏁 Kubernetes Testing Complete!" -ForegroundColor Green
}
finally {
    # Clean up port forwarding
    Write-Host "`n🧹 Cleaning up port forwarding..." -ForegroundColor Yellow
    Stop-Job -Job $portForwardJob -Force
    Remove-Job -Job $portForwardJob -Force
    Write-Host "✅ Port forwarding stopped" -ForegroundColor Green
}

Write-Host "`n💡 Additional Kubernetes Commands:" -ForegroundColor Cyan
Write-Host "View pods:     kubectl get pods -n $namespace" -ForegroundColor Gray
Write-Host "View services: kubectl get services -n $namespace" -ForegroundColor Gray
Write-Host "View logs:     kubectl logs -n $namespace deployment/$serviceName" -ForegroundColor Gray
Write-Host "Describe pod:  kubectl describe pod -n $namespace -l app=$serviceName" -ForegroundColor Gray 