# Giải thích chi tiết: Folder `infra/` và `.github/`

## Tổng quan kiến trúc

```
GitHub Push
    │
    ▼
.github/workflows/dev.yml          ← Trigger CI/CD khi push lên branch dev
    │
    ▼
.github/workflows/template.yml     ← Pipeline chính: test → build → deploy
    │
    ├── .github/actions/test_backend/     ← Chạy unit test
    ├── .github/actions/terraform_ecr/   ← Tạo ECR repository trên AWS
    ├── .github/actions/ecr/             ← Build & push Docker image lên ECR
    ├── .github/actions/terraform_ecs/   ← Deploy lên ECS
    └── .github/actions/ecs/             ← Trigger ECS service update
         │
         ▼
    infra/terraform.yaml    ← File cấu hình toàn bộ hạ tầng AWS (YAML)
    infra/main.tf           ← File Terraform đọc YAML và tạo resources
    infra/versions.tf       ← Cài đặt provider, backend S3
    infra/variables.tf      ← Biến đầu vào
    infra/outputs.tf        ← Giá trị đầu ra sau khi apply
```

---

## Folder `infra/`

Chứa toàn bộ code Terraform để tạo hạ tầng AWS.

### `infra/terraform.yaml`

File YAML duy nhất chứa **toàn bộ cấu hình tài nguyên AWS**. File `main.tf` đọc YAML này và tạo ra các resources tương ứng. Cách thiết kế này giúp người dùng chỉ cần chỉnh sửa 1 file YAML thay vì viết HCL phức tạp.

#### Cấu trúc:

```
terraform.yaml
├── vpcs              → VPC, subnet, NAT Gateway
├── sgs               → Security Groups
├── sg_rules          → Rules giữa các Security Groups
├── route53_zones     → DNS Zone
├── albs              → Application Load Balancer
├── route53_records   → DNS Records trỏ vào ALB
├── rds_databases     → RDS SQL Server
├── ecr_repositories  → ECR Docker Registry
├── namespaces        → ECS Service Discovery Namespace
└── ecs_clusters      → ECS Cluster + Services + Tasks
```

#### Chi tiết từng phần:

**VPC (`vpcs`)**
```yaml
vpcs:
  vpc_1:
    name: "chienpq137-cicd-lab-vpc"
    cidr: "10.0.0.0/16"          # Dải IP của toàn bộ mạng
    azs: ["us-east-1a", "us-east-1b"]
    public_subnets:   ["10.0.1.0/24",   "10.0.2.0/24"]    # ALB đặt ở đây
    private_subnets:  ["10.0.101.0/24", "10.0.102.0/24"]   # ECS đặt ở đây
    database_subnets: ["10.0.103.0/24", "10.0.104.0/24"]   # RDS đặt ở đây
    enable_nat_gateway: true    # ECS (private subnet) dùng NAT để ra internet
    single_nat_gateway: true    # Dùng 1 NAT duy nhất để tiết kiệm chi phí
```

> **Tại sao chia subnet?**
> - **Public subnet**: Có thể truy cập từ internet → đặt ALB
> - **Private subnet**: Không thể truy cập trực tiếp từ internet → đặt ECS (an toàn hơn)
> - **Database subnet**: Chỉ ECS mới được kết nối → đặt RDS

**Security Groups (`sgs` + `sg_rules`)**

| Security Group | Mục đích |
|---|---|
| `alb_sg` | Cho phép traffic HTTP/HTTPS từ internet vào ALB |
| `ecs_app_sg` | Cho phép ALB gửi traffic port 80 vào ECS |
| `rds_sg` | Cho phép ECS kết nối SQL Server port 1433 |

```
Internet → (port 80/443) → ALB [alb_sg]
                                │
                          (port 80) rule_1
                                │
                               ECS [ecs_app_sg]
                                │
                         (port 1433) rule_2
                                │
                               RDS [rds_sg]
```

**Route53 + ALB**
- `route53_zones`: Quản lý DNS zone `lunatik137.id.vn`
- `albs`: Tạo ALB nhận traffic HTTP port 80, forward đến ECS target group
- `route53_records`: Tạo DNS record `alb.lunatik137.id.vn` trỏ vào ALB

**RDS SQL Server (`rds_databases`)**
```yaml
rds_database_1:
  engine: "sqlserver-ex"              # SQL Server Express (free tier)
  engine_version: "15.00.4236.7.v1"  # SQL Server 2019
  instance_class: "db.t3.micro"
  port: 1433
  username: "chienpq137"
  manage_master_user_password: true   # Mật khẩu được lưu tự động vào AWS Secrets Manager
  publicly_accessible: true           # TẠM THỜI: để kết nối SSMS seed data
  storage_encrypted: true             # Mã hóa dữ liệu lưu trữ
  performance_insights_enabled: true  # Theo dõi hiệu năng
  backup_retention_period: 7          # Giữ backup 7 ngày
```

> **Lưu ý bảo mật:** `publicly_accessible: true` chỉ dùng tạm để seed data qua SSMS.
> Sau khi seed xong, đổi về `false` và chạy lại `terraform apply`.

**ECR (`ecr_repositories`)**
```yaml
ecr_repository_1:
  repository_name: "cicd-lab-app-repo"
  repository_image_scan_on_push: true  # Tự động scan lỗ hổng bảo mật khi push image
  # Lifecycle policy: chỉ giữ tối đa 5 image có tag dev-, staging-, prod-
```
> ECR là Docker Registry riêng của AWS, lưu trữ Docker images của ứng dụng.

**ECS (`ecs_clusters`)**
```yaml
ecs_cluster_1:
  cluster_name: "ecs-cluster-1"
  cluster_capacity_providers: ["FARGATE", "FARGATE_SPOT"]  # Serverless container

  services:
    service_1:
      family: "cicd-lab-app"
      cpu: 1024       # 1 vCPU
      memory: 4096    # 4 GB RAM
      desired_count: 1
      
      container_definitions:
        container_1:
          containerPort: 5000      # App chạy port 5000 bên trong container
          ecr_key: "ecr_repository_1"  # Lấy image từ ECR
          connect_db: true         # Tự động inject connection string RDS vào env
      
      connect_lb: true             # Đăng ký với ALB target group
      load_balancer:
        alb_1:
          container_port: 5000
          target_group_key: "target_group_1"
      
      enable_autoscaling: true
      autoscaling_min_capacity: 1
      autoscaling_max_capacity: 4  # Scale tối đa 4 tasks khi CPU > 70%
```

---

### `infra/main.tf`

File Terraform chính. **Không cần sửa file này thường xuyên** — nó chỉ đọc `terraform.yaml` và gọi các Terraform modules.

```hcl
locals {
  var = yamldecode(file("${path.module}/terraform.yaml"))  # Đọc YAML → HCL object
}

module "vpc"           { source = "terraform-aws-modules/vpc/aws" ... }
module "sg"            { source = "terraform-aws-modules/security-group/aws" ... }
module "alb"           { source = "terraform-aws-modules/alb/aws" ... }
module "route53_zones" { source = "terraform-aws-modules/route53/aws//modules/zones" version = "~> 3.0" }
module "route53_records" { source = "terraform-aws-modules/route53/aws//modules/records" version = "~> 3.0" }
module "rds"           { source = "terraform-aws-modules/rds/aws" ... }
module "ecr"           { source = "terraform-aws-modules/ecr/aws" ... }
module "ecs"           { source = "terraform-aws-modules/ecs/aws" ... }
```

Mỗi `module` là một thư viện Terraform có sẵn (community modules). Chúng nhận các tham số từ YAML và tạo ra resources AWS thực tế.

---

### `infra/versions.tf`

```hcl
terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 6.0"
    }
  }

  # Lưu Terraform state vào S3 thay vì máy local
  # → Nhiều người/pipeline có thể dùng chung state
  backend "s3" {
    bucket       = "chienpq137-863700943489-us-east-1-an"
    key          = "state/dev/terraform.tfstate"
    region       = "us-east-1"
    use_lockfile = false
  }
}

provider "aws" {
  region = "us-east-1"
}
```

> **Terraform State** là file JSON Terraform dùng để theo dõi resources đã tạo. Lưu trên S3 giúp CI/CD pipeline và dev local cùng đồng bộ state.

---

### `infra/variables.tf` và `infra/outputs.tf`

- **`variables.tf`**: Định nghĩa biến đầu vào (ví dụ: `environment_tags` để gắn tag tự động vào mọi resource)
- **`outputs.tf`**: Xuất giá trị sau khi apply (ví dụ: ECR repo URL, ECS cluster name) — GitHub Actions dùng các output này để build và deploy

---

## Folder `.github/`

Chứa toàn bộ cấu hình CI/CD của GitHub Actions.

```
.github/
├── workflows/
│   ├── dev.yml        ← Trigger khi push lên branch 'dev'
│   └── template.yml   ← Reusable workflow (pipeline chính)
└── actions/
    ├── test_backend/  ← Chạy tests
    ├── terraform_ecr/ ← Tạo ECR bằng Terraform
    ├── ecr/           ← Build & push Docker image
    ├── terraform_ecs/ ← Deploy ECS bằng Terraform
    └── ecs/           ← Force deploy ECS service
```

---

### `.github/workflows/dev.yml`

```yaml
# Trigger khi push lên branch 'dev'
on:
  push:
    branches: [dev]

# Gọi template.yml với các tham số
jobs:
  deploy:
    uses: ./.github/workflows/template.yml
    with:
      environment: dev
      aws-region: us-east-1
    secrets:
      AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
      AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
      AWS_SESSION_TOKEN: ${{ secrets.AWS_SESSION_TOKEN }}
```

> `dev.yml` chỉ là file "gọi" — toàn bộ logic nằm trong `template.yml`.

---

### `.github/workflows/template.yml`

Pipeline chính, chạy lần lượt 5 bước:

```
[1] test_backend  →  [2] terraform_ecr  →  [3] ecr  →  [4] terraform_ecs  →  [5] ecs
  (unit test)          (tạo ECR repo)      (push image)    (update ECS)       (restart)
```

| Bước | Action | Mục đích |
|---|---|---|
| 1 | `test_backend` | Chạy `dotnet test` để đảm bảo code không bị lỗi |
| 2 | `terraform_ecr` | `terraform apply` để tạo ECR repository nếu chưa có |
| 3 | `ecr` | `docker build` + `docker push` image lên ECR |
| 4 | `terraform_ecs` | `terraform apply` để cập nhật ECS task definition |
| 5 | `ecs` | Trigger ECS service để pull image mới và restart |

---

### `.github/actions/test_backend/action.yml`

```yaml
# Chạy dotnet test trong thư mục Project_Group3
- name: Run tests
  run: dotnet test
  working-directory: ./Project_Group3
```

---

### `.github/actions/terraform_ecr/action.yml`

```yaml
# 1. Cấu hình AWS credentials
# 2. cd infra/ → terraform init → terraform apply
# 3. Đọc output ECR repo URL từ terraform output
# Output: ecr-app-repo (URL của ECR repository)
```

> Sau bước này, ECR repository `cicd-lab-app-repo` đã tồn tại trên AWS.

---

### `.github/actions/ecr/action.yml`

```yaml
# 1. aws ecr get-login-password | docker login
# 2. docker build -t <ecr-repo>:latest .   (đọc Dockerfile trong Project_Group3/)
# 3. docker push <ecr-repo>:latest
```

> Docker image được build từ `Project_Group3/Dockerfile` và đẩy lên ECR với tag `latest`.

---

### `.github/actions/terraform_ecs/action.yml`

```yaml
# 1. cd infra/ → terraform apply (cập nhật ECS task definition với image mới)
# Output: 
#   - cluster-name (tên ECS cluster)
#   - app (tên ECS service)
```

---

### `.github/actions/ecs/action.yml`

```yaml
# aws ecs update-service --force-new-deployment
# → ECS tự động pull image mới từ ECR và restart containers
```

---

## Luồng deploy hoàn chỉnh

```
Developer push code lên branch 'dev'
    │
    ▼
GitHub Actions kích hoạt dev.yml
    │
    ▼
[Bước 1] dotnet test → nếu lỗi: dừng pipeline
    │
    ▼
[Bước 2] terraform apply (ECR) → tạo repo nếu chưa có
    │
    ▼
[Bước 3] docker build + push → image mới trên ECR với tag 'latest'
    │
    ▼
[Bước 4] terraform apply (ECS) → cập nhật task definition
    │
    ▼
[Bước 5] aws ecs update-service → ECS pull image mới, restart app
    │
    ▼
App chạy trên ECS Fargate, nhận traffic qua ALB
URL: http://alb.lunatik137.id.vn
```

---

## Các lệnh Terraform thường dùng

```powershell
cd "D:\PRN222\Group Project\PRN222\infra"

# Khởi tạo (chỉ chạy 1 lần hoặc khi thêm module mới)
terraform init

# Xem trước những gì sẽ thay đổi
terraform plan

# Áp dụng thay đổi (toàn bộ)
terraform apply -auto-approve

# Áp dụng chỉ 1 module cụ thể
terraform apply -target="module.rds" -auto-approve

# Xem giá trị output
terraform output

# Xóa toàn bộ hạ tầng (NGUY HIỂM!)
terraform destroy -auto-approve
```

---

## Thứ tự apply khi deploy lần đầu

Do một số resources phụ thuộc vào nhau, phải apply theo thứ tự:

```powershell
# Bước 1: Tạo networking và registry
terraform apply -target="module.vpc" -target="module.sg" -target="module.ecr" -target="module.route53_zones" -target="module.acm" -auto-approve

# Bước 2: Apply toàn bộ còn lại (ALB, Route53 records, RDS, ECS)
terraform apply -auto-approve
```

> **Lý do:** ALB cần ACM certificate ARN (chỉ biết sau khi ACM được tạo), Route53 records cần zone_id (chỉ biết sau khi zone được tạo).
