locals {
  var = yamldecode(file("${path.module}/terraform.yaml"))
}

################################################################################
# VPC
################################################################################
module "vpc" {
  source = "terraform-aws-modules/vpc/aws"

  for_each = local.var.vpcs

  create_vpc = try(each.value.create, true)

  name             = try(each.value.name, null)
  cidr             = try(each.value.cidr, null)
  enable_ipv6      = try(each.value.enable_ipv6, false)
  instance_tenancy = try(each.value.instance_tenancy, null)

  azs              = try(each.value.azs, [])
  private_subnets  = try(each.value.private_subnets, [])
  public_subnets   = try(each.value.public_subnets, [])
  database_subnets = try(each.value.database_subnets, [])

  enable_nat_gateway     = try(each.value.enable_nat_gateway, false)
  single_nat_gateway     = try(each.value.single_nat_gateway, false)
  one_nat_gateway_per_az = try(each.value.one_nat_gateway_per_az, false)

  enable_dns_hostnames = try(each.value.enable_dns_hostnames, true)
  enable_dns_support   = try(each.value.enable_dns_support, true)

  tags = try(merge(each.value.tags, var.environment_tags), {})
}

################################################################################
# Security Group
################################################################################
module "sg" {
  source = "terraform-aws-modules/security-group/aws"

  for_each = local.var.sgs

  create = try(each.value.create, true)

  name        = try(each.value.name, null)
  description = try(each.value.description, null)
  vpc_id      = try(module.vpc[each.value.vpc_key].vpc_id, null)

  ingress_cidr_blocks = try(each.value.ingress_cidr_blocks, [])
  ingress_rules       = try(coalesce(each.value.ingress_rules, []), [])

  egress_cidr_blocks = try(each.value.egress_cidr_blocks, [])
  egress_rules       = try(coalesce(each.value.egress_rules, []), [])

  tags = try(merge(each.value.tags, var.environment_tags), {})
}

resource "aws_security_group_rule" "this" {
  for_each = local.var.sg_rules

  security_group_id        = try(module.sg[each.value.sg_key].security_group_id, null)
  source_security_group_id = try(module.sg[each.value.source_sg_key].security_group_id, null)
  type                     = try(each.value.type, null)
  from_port                = try(each.value.from_port, null)
  to_port                  = try(each.value.to_port, null)
  protocol                 = try(each.value.protocol, null)
  description              = try(each.value.description, null)
}

################################################################################
# Route 53 Zone
################################################################################
module "route53_zones" {
  source = "terraform-aws-modules/route53/aws//modules/zones"

  for_each = local.var.route53_zones

  create = try(each.value.create, true)

  zones = try(each.value.zones, {})
  tags  = try(merge(each.value.tags, var.environment_tags), {})
}

################################################################################
# ACM
################################################################################
module "acm" {
  source = "terraform-aws-modules/acm/aws"

  for_each = local.var.acms

  create_certificate = try(each.value.create, true)

  domain_name               = try(values(module.route53_zones[each.value.zone_key].route53_zone_name)[0], "")
  zone_id                   = try(values(module.route53_zones[each.value.zone_key].route53_zone_zone_id)[0], null)
  export                    = try(each.value.export, null)
  validation_method         = try(each.value.validation_method, null)
  key_algorithm             = try(each.value.key_algorithm, null)
  subject_alternative_names = try(["*.${values(module.route53_zones[each.value.zone_key].route53_zone_name)[0]}"], [])

  tags = try(merge(each.value.tags, var.environment_tags), {})
}

################################################################################
# ALB
################################################################################
module "alb" {
  source = "terraform-aws-modules/alb/aws"

  for_each = local.var.albs

  create = try(each.value.create, true)

  load_balancer_type    = try(each.value.load_balancer_type, null)
  name                  = try(each.value.name, null)
  internal              = try(each.value.internal, false)
  ip_address_type       = try(each.value.ip_address_type, null)
  vpc_id                = try(module.vpc[each.value.vpc_key].vpc_id, null)
  ipam_pools            = try(coalesce(each.value.ipam_pools, {}), {})
  subnets               = try(module.vpc[each.value.vpc_key].public_subnets, [])
  security_groups       = try([module.sg[each.value.sg_key].security_group_id], [])
  create_security_group = try(each.value.create_security_group, false)

  listeners = try({
    for k, v in each.value.listeners : k => merge(v, {
      certificate_arn = try(v.acm_key, null) != null ? module.acm[v.acm_key].acm_certificate_arn : null
    })
  }, {})

  target_groups = try({
    for k, v in each.value.target_groups : k => merge(v, {
      vpc_id = module.vpc[v.vpc_key].vpc_id
    })
  }, {})

  tags = try(merge(each.value.tags, var.environment_tags), {})
}

################################################################################
# Route 53 Record
################################################################################
module "route53_records" {
  source = "terraform-aws-modules/route53/aws//modules/records"

  for_each = local.var.route53_records

  create = try(each.value.create, true)

  zone_id = try(values(module.route53_zones[each.value.zone_key].route53_zone_zone_id)[0], null)
  records = try([
    for record in each.value.records : merge(record, {
      alias = try(merge(record.alias, {
        name    = try(record.alb_key, null) != null ? module.alb[record.alb_key].dns_name : null
        zone_id = try(record.alb_key, null) != null ? module.alb[record.alb_key].zone_id : null
      }), record.alias)
    })
  ], [])
}

################################################################################
# RDS
################################################################################
data "aws_kms_key" "master_user_secret_kms_key_id" {
  for_each = local.var.rds_databases

  key_id = try(each.value.master_user_secret_kms_key_id, null)
}

data "aws_kms_key" "performance_insights_kms_key_id" {
  for_each = local.var.rds_databases

  key_id = try(each.value.performance_insights_kms_key_id, null)
}

data "aws_kms_key" "kms_key_id" {
  for_each = local.var.rds_databases

  key_id = try(each.value.kms_key_id, null)
}

module "rds" {
  source = "terraform-aws-modules/rds/aws"

  for_each = local.var.rds_databases

  create_db_instance = try(each.value.create, true)

  engine                   = try(each.value.engine, null)
  engine_version           = try(each.value.engine_version, null)
  engine_lifecycle_support = try(each.value.engine_lifecycle_support, null)
  multi_az                 = try(each.value.multi_az, false)

  identifier                    = try(each.value.identifier, null)
  username                      = try(each.value.username, null)
  manage_master_user_password   = try(each.value.manage_master_user_password, true)
  master_user_secret_kms_key_id = try(data.aws_kms_key.master_user_secret_kms_key_id[each.key].arn, null)
  instance_class                = try(each.value.instance_class, null)

  storage_type          = try(each.value.storage_type, null)
  allocated_storage     = try(each.value.allocated_storage, null)
  iops                  = try(each.value.iops, null)
  storage_throughput    = try(each.value.storage_throughput, null)
  max_allocated_storage = try(each.value.max_allocated_storage, null)
  dedicated_log_volume  = try(each.value.dedicated_log_volume, false)

  create_db_subnet_group      = try(each.value.create_db_subnet_group, true)
  db_subnet_group_name        = try(each.value.db_subnet_group_name, null)
  db_subnet_group_description = try(each.value.db_subnet_group_description, null)
  subnet_ids                  = try(module.vpc[each.value.vpc_key].database_subnets, [])

  network_type           = try(each.value.network_type, null)
  publicly_accessible    = try(each.value.publicly_accessible, false)
  vpc_security_group_ids = try([module.sg[each.value.sg_key].security_group_id], [])
  ca_cert_identifier     = try(each.value.ca_cert_identifier, null)
  port                   = try(each.value.port, null)

  iam_database_authentication_enabled = try(each.value.iam_database_authentication_enabled, false)
  create_monitoring_role              = try(each.value.create_monitoring_role, true)
  monitoring_role_name                = try(each.value.monitoring_role_name, null)
  monitoring_role_description         = try(each.value.monitoring_role_description, null)

  performance_insights_enabled          = try(each.value.performance_insights_enabled, true)
  performance_insights_retention_period = try(each.value.performance_insights_retention_period, null)
  performance_insights_kms_key_id       = try(data.aws_kms_key.performance_insights_kms_key_id[each.key].arn, null)
  monitoring_interval                   = try(each.value.monitoring_interval, null)
  create_cloudwatch_log_group           = try(each.value.create_cloudwatch_log_group, true)
  enabled_cloudwatch_logs_exports       = try(each.value.enabled_cloudwatch_logs_exports, [])

  db_name              = try(each.value.db_name, null)
  family               = try(each.value.family, null)
  major_engine_version = try(each.value.major_engine_version, null)

  backup_retention_period = try(each.value.backup_retention_period, null)
  backup_window           = try(each.value.backup_window, null)
  copy_tags_to_snapshot   = try(each.value.copy_tags_to_snapshot, true)
  storage_encrypted       = try(each.value.storage_encrypted, true)
  kms_key_id              = try(data.aws_kms_key.kms_key_id[each.key].arn, null)

  auto_minor_version_upgrade = try(each.value.auto_minor_version_upgrade, true)
  maintenance_window         = try(each.value.maintenance_window, null)
  deletion_protection        = try(each.value.deletion_protection, false)

  tags = try(merge(each.value.tags, var.environment_tags), {})
}

################################################################################
# ECR
################################################################################
module "ecr" {
  source = "terraform-aws-modules/ecr/aws"

  for_each = local.var.ecr_repositories

  create = try(each.value.create, true)

  repository_name                                  = try(each.value.repository_name, null)
  repository_image_tag_mutability                  = try(each.value.repository_image_tag_mutability, null)
  repository_image_tag_mutability_exclusion_filter = try(each.value.repository_image_tag_mutability_exclusion_filter, null)
  repository_encryption_type                       = try(each.value.repository_encryption_type, null)
  repository_image_scan_on_push                    = try(each.value.repository_image_scan_on_push, true)
  repository_lifecycle_policy                      = try(jsonencode(each.value.repository_lifecycle_policy), null)

  tags = try(merge(each.value.tags, var.environment_tags), {})
}

################################################################################
# ECS
################################################################################
resource "aws_service_discovery_http_namespace" "this" {
  for_each = local.var.namespaces

  name        = try(each.value.name, null)
  description = try(each.value.description, null)
  tags        = try(merge(each.value.tags, var.environment_tags), {})
}

data "aws_kms_key" "ecs" {
  for_each = local.var.ecs_clusters

  key_id = try(each.value.kms_key_id, null)
}

module "ecs" {
  source = "terraform-aws-modules/ecs/aws"

  for_each = local.var.ecs_clusters

  create = try(each.value.create, true)

  cluster_name                       = try(each.value.cluster_name, null)
  cluster_service_connect_defaults   = try({ namespace = aws_service_discovery_http_namespace.this[each.key].arn }, null)
  default_capacity_provider_strategy = try(each.value.default_capacity_provider_strategy, {})
  cluster_setting                    = try(each.value.cluster_setting, [])
  cluster_tags                       = try(each.value.cluster_tags, {})

  cluster_configuration = try(merge(each.value.cluster_configuration, {
    execute_command_configuration = try(merge(each.value.cluster_configuration.execute_command_configuration, {
      kms_key_id = try(data.aws_kms_key.ecs[each.key].arn, null)
    }), {})

    # managed_storage_configuration = try({
    #   fargate_ephemeral_storage_kms_key_id = try(data.aws_kms_key.ecs[each.key].arn, null)
    #   kms_key_id                           = try(data.aws_kms_key.ecs[each.key].arn, null)
    # }, {})
  }), {})

  services = try({
    for k, v in each.value.services : k => {
      family                   = try(v.family, null)
      requires_compatibilities = try(v.requires_compatibilities, [])
      runtime_platform         = try(v.runtime_platform, {})
      network_mode             = try(v.network_mode, null)
      cpu                      = try(v.cpu, null)
      memory                   = try(v.memory, null)

      tasks_iam_role_name        = try(v.tasks_iam_role_name, null)
      tasks_iam_role_description = try(v.tasks_iam_role_description, null)
      tasks_iam_role_policies    = try(v.tasks_iam_role_policies, {})
      tasks_iam_role_tags        = try(v.tasks_iam_role_tags, {})
      task_exec_secret_arns      = try([module.rds[v.rds_key].db_instance_master_user_secret_arn], [])
      enable_fault_injection     = try(v.enable_fault_injection, null)

      container_definitions = {
        for k1, v1 in v.container_definitions : k1 => {
          name                   = try(v1.name, null)
          essential              = try(v1.essential, null)
          image                  = try("${module.ecr[v1.ecr_key].repository_url}:latest", null)
          repositoryCredentials  = try(v1.repositoryCredentials, null)
          portMappings           = try(v1.portMappings, [])
          readonlyRootFilesystem = try(v1.readonlyRootFilesystem, null)

          cpu               = try(v1.cpu, null)
          memory            = try(v1.memory, null)
          memoryReservation = try(v1.memoryReservation, null)

          secrets = try(v1.connect_db, false) == true ? try(concat(v1.secrets, [
            { name = "DB_PASSWORD"
            valueFrom = try("${module.rds[v.rds_key].db_instance_master_user_secret_arn}:password::", null) },
            { name = "DB_USERNAME"
            valueFrom = try("${module.rds[v.rds_key].db_instance_master_user_secret_arn}:username::", null) }
          ]), []) : try(v1.secrets, [])

          environment = try(v1.connect_db, false) == true ? try(concat(v1.environment, [
            { name = "DB_HOST"
            value = try(module.rds[v.rds_key].db_instance_endpoint, null) },
            { name = "DB_NAME"
            value = try(module.rds[v.rds_key].db_instance_name, null) }
          ]), []) : try(v1.environment, [])

          environmentFiles = try(v1.environmentFiles, [])
          logConfiguration = try(v1.logConfiguration, {})
          restartPolicy    = try(v1.restartPolicy, {})
          healthCheck      = try(v1.healthCheck, null)
          startTimeout     = try(v1.startTimeout, null)
          stopTimeout      = try(v1.stopTimeout, null)

          entryPoint       = try(v1.entryPoint, [])
          command          = try(v1.command, [])
          workingDirectory = try(v1.workingDirectory, null)
          ulimits          = try(v1.ulimits, [])
          dockerLabels     = try(v1.dockerLabels, {})

          mountPoints = try(v.mountPoints, [])
          volumesFrom = try(v.volumesFrom, [])
        }
      }

      ephemeral_storage = try(v.ephemeral_storage, {})
      volume            = try(v.volume, {})
      task_tags         = try(v.task_tags, {})

      name                       = try(v.name, null)
      capacity_provider_strategy = try(v.capacity_provider_strategy, {})
      platform_version           = try(v.platform_version, null)
      enable_execute_command     = try(v.enable_execute_command, null)

      scheduling_strategy               = try(v.scheduling_strategy, null)
      desired_count                     = try(v.desired_count, null)
      availability_zone_rebalancing     = try(v.availability_zone_rebalancing, null)
      health_check_grace_period_seconds = try(v.health_check_grace_period_seconds, null)

      deployment_controller              = try(v.deployment_controller, {})
      deployment_configuration           = try(v.deployment_configuration, {})
      deployment_minimum_healthy_percent = try(v.deployment_minimum_healthy_percent, null)
      deployment_maximum_percent         = try(v.deployment_maximum_percent, null)

      deployment_circuit_breaker = try(v.deployment_circuit_breaker, {})
      alarms                     = try(v.alarms, null)

      subnet_ids            = try(module.vpc[v.vpc_key].private_subnets, [])
      security_group_ids    = try([module.sg[v.sg_key].security_group_id], [])
      create_security_group = try(v.create_security_group, null)
      assign_public_ip      = try(v.assign_public_ip, null)

      service_connect_configuration = try(merge(v.service_connect_configuration, {
        namespace = try(aws_service_discovery_http_namespace.this[each.key].arn, null)
      }), {})

      load_balancer = try(v.connect_lb, false) == true ? try({
        for k1, v1 in v.load_balancer : k1 => merge(v1, {
          target_group_arn = try(module.alb[v1.alb_key].target_groups[v1.target_group_key].arn, null)
        })
      }, {}) : {}

      enable_autoscaling       = try(v.enable_autoscaling, null)
      autoscaling_min_capacity = try(v.autoscaling_min_capacity, null)
      autoscaling_max_capacity = try(v.autoscaling_max_capacity, null)
      autoscaling_policies     = try(v.autoscaling_policies, {})
      
      service_tags = try(v.service_tags, {})
      tags         = try(v.tags, {})
    }
  }, {})

  tags = try(merge(each.value.tags, var.environment_tags), {})
}