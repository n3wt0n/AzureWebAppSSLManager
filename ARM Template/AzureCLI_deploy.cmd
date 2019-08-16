az group create -n YOUR_RESOURCE_GROUP_NAME -l EastAsia

az group deployment create --resource-group YOUR_RESOURCE_GROUP_NAME --template-file template.json --parameters @parameters.json --verbose 