resource "aws_vpc" "this" {
  cidr_block = var.vpc_cidr

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-vpc" }
  )
}

resource "aws_subnet" "public" {
  count             = var.public_subnet_enabled ? 1 : 0
  vpc_id            = aws_vpc.this.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 4, count.index)
  availability_zone = element(var.availability_zones, count.index)
  map_public_ip_on_launch = var.auto_assign_public_ip

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-public-subnet-${count.index}" }
  )
}
