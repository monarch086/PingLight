resource "aws_vpc" "this" {
  cidr_block = var.vpc_cidr

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-vpc" }
  )
}

resource "aws_subnet" "public" {
  count             = var.az_count
  vpc_id            = aws_vpc.this.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index)
  availability_zone = data.aws_availability_zones.available.names[count.index]
  map_public_ip_on_launch = var.auto_assign_public_ip

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-public-subnet-${count.index}" }
  )
}

resource "aws_subnet" "private" {
    count             = var.az_count
    cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index + var.az_count)
    availability_zone = data.aws_availability_zones.available.names[count.index]
    vpc_id            = aws_vpc.this.id

    tags = merge(
      var.additional_tags,
      { Name = "${var.environment}-private-subnet-${count.index}" }
    )
}

# Internet Gateway
resource "aws_internet_gateway" "gw" {
  vpc_id = aws_vpc.this.id

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-igw" }
  )
}

# Public Route Table
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.this.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.gw.id
  }

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-public-route-table" }
  )
}

resource "aws_route_table_association" "public" {
  count          = var.az_count
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# NAT Gateway (Single Instance for Cost Optimization)
resource "aws_eip" "nat" {
  domain = "vpc"
  depends_on = [aws_internet_gateway.gw]
}

resource "aws_nat_gateway" "nat" {
  subnet_id     = aws_subnet.public[0].id
  allocation_id = aws_eip.nat.id

  tags = merge(
    var.additional_tags,
    { Name = "${var.environment}-nat" }
  )
}

# Private Route Table
resource "aws_route_table" "private" {
    vpc_id = aws_vpc.this.id

    route {
      cidr_block     = "0.0.0.0/0"
      nat_gateway_id = aws_nat_gateway.nat.id
    }

    tags = merge(
      var.additional_tags,
      { Name = "${var.environment}-private-route-table" }
    )
}

resource "aws_route_table_association" "private" {
    count          = var.az_count
    subnet_id      = aws_subnet.private[count.index].id
    route_table_id = aws_route_table.private.id
}

data "aws_availability_zones" "available" {
  state = "available"
}
