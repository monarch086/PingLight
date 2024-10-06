output "db_instance_endpoint" {
  value = aws_db_instance.this.endpoint
}

output "db_instance_name" {
  value = aws_db_instance.this.db_name
}