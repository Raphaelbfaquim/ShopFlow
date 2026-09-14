variable "location" {
  type    = string
  default = "eastus"
}

variable "prefix" {
  type    = string
  default = "shopflow"
}

variable "sql_admin_login" {
  type = string
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${var.prefix}"
  location = var.location
}

resource "azurerm_key_vault" "main" {
  name                       = "kv-${var.prefix}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = false
  soft_delete_retention_days = 7
}

data "azurerm_client_config" "current" {}

resource "azurerm_user_assigned_identity" "api" {
  name                = "id-${var.prefix}-api"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
}

resource "azurerm_key_vault_access_policy" "api" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_user_assigned_identity.api.principal_id

  secret_permissions = ["Get", "List"]
}

resource "azurerm_mssql_server" "main" {
  name                         = "sql-${var.prefix}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
}

resource "azurerm_mssql_database" "main" {
  name      = "sqldb-${var.prefix}"
  server_id = azurerm_mssql_server.main.id
  sku_name  = "Basic"
}

resource "azurerm_storage_account" "main" {
  name                     = replace("${var.prefix}st", "-", "")
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_service_plan" "main" {
  name                = "plan-${var.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-${var.prefix}-api"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT   = "Production"
    AzureKeyVault__Uri       = azurerm_key_vault.main.vault_uri
    Messaging__Provider      = "InMemory"
    AzureStorage__Enabled    = "true"
    AZURE_CLIENT_ID          = azurerm_user_assigned_identity.api.client_id
  }
}

resource "azurerm_linux_function_app" "worker" {
  name                = "func-${var.prefix}-worker"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id
  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }
}

resource "azurerm_api_management" "main" {
  name                = "apim-${var.prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  publisher_name      = "ShopFlow"
  publisher_email     = "dev@shopflow.dev"
  sku_name            = "Consumption_0"
}

output "api_hostname" {
  value = azurerm_linux_web_app.api.default_hostname
}

output "key_vault_uri" {
  value = azurerm_key_vault.main.vault_uri
}
