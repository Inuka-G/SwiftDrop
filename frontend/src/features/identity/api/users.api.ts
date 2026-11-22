import { apiClient } from '../../../lib/axios'
import { USE_MOCKS } from '../../../lib/constants'
import type { UserDto, UserPageDto } from '../types/users.types'

const mockUsers: UserDto[] = [
  { id: 'b1000000-0000-4000-8000-000000000001', fullName: 'Nimal Perera', email: 'nimal.p@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-15T08:00:00Z' },
  { id: 'b1000000-0000-4000-8000-000000000002', fullName: 'Sunimal Silva', email: 'sunimal.s@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-16T08:00:00Z' },
  { id: 'b1000000-0000-4000-8000-000000000003', fullName: 'Nuwan Fernando', email: 'nuwan.f@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-17T08:00:00Z' },
  { id: 'b1000000-0000-4000-8000-000000000004', fullName: 'Kamal Perera', email: 'kamal.p@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-18T08:00:00Z' },
  { id: 'b1000000-0000-4000-8000-000000000005', fullName: 'Ruwan Jayasinghe', email: 'ruwan.j@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-19T08:00:00Z' },
  { id: 'b1000000-0000-4000-8000-000000000006', fullName: 'Kasun Bandara', email: 'kasun.b@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-20T08:00:00Z' },
  { id: 'b1000000-0000-4000-8000-000000000007', fullName: 'Roshan Lokuge', email: 'roshan.l@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-01-21T08:00:00Z' },
  { id: 'c2000000-0000-4000-8000-000000000001', fullName: 'Dinesh Mendis', email: 'dinesh.m@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-02-02T08:00:00Z' },
  { id: 'c2000000-0000-4000-8000-000000000002', fullName: 'Chathura Silva', email: 'chathura.s@swiftdrop.local', role: 'COURIER', isActive: true, authProvider: 'LOCAL', createdAtUtc: '2026-02-03T08:00:00Z' },
  { id: 'c2000000-0000-4000-8000-000000000003', fullName: 'Madhavi Perera', email: 'madhavi.p@swiftdrop.local', role: 'COURIER', isActive: false, authProvider: 'LOCAL', createdAtUtc: '2026-02-04T08:00:00Z' },
]

export function getMockCourierUser(id: string): UserDto | undefined {
  return mockUsers.find((user) => user.id === id)
}

export async function searchCourierUsers(search: string, page = 1, pageSize = 20): Promise<UserPageDto> {
  if (USE_MOCKS) {
    const query = search.trim().toLocaleLowerCase()
    const items = mockUsers.filter((user) => user.role === 'COURIER' && user.isActive &&
      (!query || `${user.fullName} ${user.email}`.toLocaleLowerCase().includes(query)))
    return { items: items.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: items.length }
  }
  const { data } = await apiClient.get<UserPageDto>('/users', {
    params: { role: 'COURIER', isActive: true, search, page, pageSize },
  })
  return data
}
