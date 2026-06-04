#!/bin/bash

# Script de Setup para Mac - Remove Quarentena e Compila
# Usage: bash setup-mac.sh

echo "🍎 Setup Compra Programada de Ações no Mac"
echo "================================"
echo ""

# 1. Remove quarentena da pasta atual
echo "🔓 Removendo quarentena dos arquivos..."
xattr -rd com.apple.quarantine . 2>/dev/null || true
echo "✅ Quarentena removida"
echo ""

# 2. Limpa cache do .NET
echo "🧹 Limpando cache do .NET..."
rm -rf ~/.dotnet/NuGet/v3-cache 2>/dev/null || true
rm -rf ~/.nuget/packages 2>/dev/null || true
rm -rf bin obj 2>/dev/null || true
echo "✅ Cache limpo"
echo ""

# 3. Restaura dependências
echo "📦 Restaurando dependências..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Erro ao restaurar dependências"
    exit 1
fi
echo "✅ Dependências restauradas"
echo ""

# 4. Compila
echo "🔨 Compilando projeto..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Erro ao compilar"
    exit 1
fi
echo "✅ Compilação concluída"
echo ""

# 5. Rodar testes
echo "🧪 Rodando testes (70+)..."
dotnet test tests/Tests.csproj -v normal
if [ $? -ne 0 ]; then
    echo "⚠️ Alguns testes falharam"
else
    echo "✅ Todos os testes passaram!"
fi
echo ""

echo "🏆 Setup concluído com sucesso!"
echo ""
echo "Próximos passos:"
echo "1. Frontend: cd frontend && npm install && npm start"
echo "2. API: dotnet run --project src/API/API.csproj"
echo "3. Docker: docker-compose up -d"
