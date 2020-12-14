# Register Resource Providers if they're not already registered
Register-AzResourceProvider -ProviderNamespace "microsoft.web"
Register-AzResourceProvider -ProviderNamespace "microsoft.storage"
Register-AzResourceProvider -ProviderNamespace "microsoft.insights"

# Create a resource group for the function app
New-AzResourceGroup -Name "YOUR_RESOURCE_GROUP_NAME" -Location 'West Europe'

# Deploy the template
New-AzResourceGroupDeployment -ResourceGroupName "YOUR_RESOURCE_GROUP_NAME" -TemplateFile template.json -TemplateParameterFile parameters.json -Verbose