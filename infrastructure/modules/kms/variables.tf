variable "description" {}
variable "deletion_window_in_days" {}
variable "enable_key_rotation" {}
variable "is_enabled" {}
variable "key_usage" {}
variable "multi_region" {}
variable "key_statements" {
  type = list(object({
    sid        = string
    actions    = list(string)
    resources  = list(string)
    principals = list(object({
      type        = string
      identifiers = list(string)
    }))
  }))
}
variable "alias" {}
variable "additional_tags" {}
