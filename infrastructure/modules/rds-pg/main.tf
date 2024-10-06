resource "aws_db_instance" "db_instance" {
  allocated_storage      = var.allocated_storage
  max_allocated_storage  = var.max_allocated_storage
  engine                 = "postgres"
  engine_version         = var.engine_version
  instance_class         = var.instance_class
  db_name                = var.db_name
  username               = var.master_username
  password               = var.custom_user_password
  publicly_accessible    = var.publicly_accessible
  multi_az               = var.multi_az
  storage_type           = var.storage_type
  vpc_security_group_ids = var.allowed_security_groups
  db_subnet_group_name   = var.subnet_ids

  tags = var.additional_tags
}
