export interface UserDto {
  id: string
  fullName: string
  email: string
  role: string
  isActive: boolean
  authProvider: string
  createdAtUtc: string
}

export interface UserPageDto {
  items: UserDto[]
  page: number
  pageSize: number
  totalCount: number
}

export interface CourierUserSearch {
  search: string
  page?: number
  pageSize?: number
}
