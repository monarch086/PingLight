resource "aws_kms_key" "this" {
  description             = var.description
  deletion_window_in_days = var.deletion_window_in_days
  enable_key_rotation     = var.enable_key_rotation
  is_enabled              = var.is_enabled
  key_usage               = var.key_usage
  multi_region            = var.multi_region

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = var.key_statements
  })

  tags = var.additional_tags
}

resource "aws_kms_alias" "this" {
  name          = "alias/${var.alias}"
  target_key_id = aws_kms_key.this.key_id
}
