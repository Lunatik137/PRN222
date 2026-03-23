output "ecr_repo" {
  description = "ECR Repository URL"
  value       = { for k, v in module.ecr : k => v.repository_url }
}

output "ecs_info" {
  description = "ECS Services Info including service name, task definition ARN, and container names"
  value = {
    for k, v in module.ecs : k => {
      cluster_name = v.cluster_name

      services = {
        for k1, v1 in v.services : k1 => {
          service_name        = v1.name
          task_definition_arn = v1.task_definition_arn
          container_names     = join(",", [for k2, v2 in v1.container_definitions : v2.container_definition.name])
        }
      }
    }
  }
}