variable "db_name" {}
variable "engine_version" {}
variable "instance_class" {}
variable "allocated_storage" {}
variable "max_allocated_storage" {}
variable "publicly_accessible" {
  default = false
}
variable "multi_az" {
  default = false
}
variable "storage_type" {}
variable "master_username" {}
variable "master_userpassword" {}
variable "vpc_id" {}
variable "subnet_ids" {}
variable "additional_tags" {
  type = map(string)
  default = {}
}
