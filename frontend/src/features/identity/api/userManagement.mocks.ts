import type { UserDto, UserPageDto } from '../types/users.types'
import type { UserDirectoryFilters } from '../types/userManagement.types'

const mockUsers: UserDto[] = [
  ['Marcus S.', 'marcus.s@swiftdrop.lk', 'ADMIN', true], ['Sarah Jenkins', 'sarah.j@swiftdrop.lk', 'ADMIN', true],
  ['Nuwan Pradeep', 'nuwan.p@swiftdrop.lk', 'ADMIN', true], ['Kamal Perera', 'kamal.p@swiftdrop.lk', 'COURIER', true],
  ['Nimal Perera', 'nimal.p@swiftdrop.lk', 'COURIER', true], ['Dinesh Weerasinghe', 'dinesh.w@retailhub.lk', 'MERCHANT', true],
  ['Anoma Fernando', 'anoma.f@gmail.com', 'CUSTOMER', true], ['Roshan Lokuge', 'roshan.l@gmail.com', 'CUSTOMER', false],
  ['Sunimal Silva', 'sunimal.s@swiftdrop.lk', 'DISPATCHER', true], ['Ceylon Artisan Spices Ltd', 'merchants@ceylonartisan.lk', 'MERCHANT', true],
  ['Tharindu Jayasuriya', 'tharindu.j@gmail.com', 'CUSTOMER', true], ['Kasun Bandara', 'kasun.b@swiftdrop.lk', 'COURIER', false],
]
  .map(([fullName, email, role, isActive], index) => ({
    id: `a1000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
    fullName: String(fullName), email: String(email), role: String(role), isActive: Boolean(isActive),
    authProvider: 'LOCAL', createdAtUtc: new Date(Date.UTC(2026, 9, 27 - index)).toISOString(),
  }))

const sampleCustomers = ['Anjali Perera', 'Amal Jayawardena', 'Dinuka Silva', 'Thilini Fernando', 'Ishara Bandara', 'Kavindu Dias', 'Nadeesha Kumari', 'Rashmi De Silva', 'Sahan Wijesinghe', 'Piumi Gunasekara', 'Kavisha Perera', 'Ravindu Silva']
for (let index = 0; index < 36; index++) {
  const name = sampleCustomers[index % sampleCustomers.length]
  mockUsers.push({
    id: `a1000000-0000-4000-8000-${String(index + 13).padStart(12, '0')}`,
    fullName: `${name} ${Math.floor(index / sampleCustomers.length) + 1}`,
    email: `${name.toLowerCase().replaceAll(' ', '.')}${Math.floor(index / sampleCustomers.length) + 1}@gmail.com`,
    role: index % 9 === 0 ? 'COURIER' : index % 11 === 0 ? 'DISPATCHER' : index % 13 === 0 ? 'MERCHANT' : 'CUSTOMER',
    isActive: index % 8 !== 0,
    authProvider: index % 4 === 0 ? 'GOOGLE' : 'LOCAL',
    createdAtUtc: new Date(Date.UTC(2026, 9, 15 - index)).toISOString(),
  })
}

function mockStatus(key: string) {
  const mode = new URLSearchParams(window.location.search).get('usersMock')
  const status = mode?.startsWith(`${key}-`) ? Number(mode.slice(key.length + 1)) : undefined
  if (status) throw Object.assign(new Error('Mock user management error'), { response: { status } })
}

export async function getUsersMock(filters: UserDirectoryFilters, page: number, pageSize = filters.pageSize ?? 10): Promise<UserPageDto> {
  mockStatus('users')
  const search = filters.search.trim().toLocaleLowerCase()
  const users = mockUsers.filter((user) => (!search || `${user.fullName} ${user.email}`.toLocaleLowerCase().includes(search)) &&
    (!filters.role || user.role === filters.role) && (filters.isActive === '' || user.isActive === (filters.isActive === 'true')))
  return { items: users.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: users.length }
}

export function getUserCountsMock() {
  let total: number | null = mockUsers.length
  let activeAdmins: number | null = mockUsers.filter((user) => user.role === 'ADMIN' && user.isActive).length
  try { mockStatus('counts-total') } catch { total = null }
  try { mockStatus('counts-admin') } catch { activeAdmins = null }
  return { total, activeAdmins }
}

export async function updateUserRoleMock(id: string, role: string): Promise<UserDto> {
  mockStatus('role')
  const user = mockUsers.find((item) => item.id === id)
  if (!user) throw Object.assign(new Error('Not found'), { response: { status: 404 } })
  if (user.role === 'ADMIN' && user.isActive && role !== 'ADMIN' && mockUsers.filter((item) => item.role === 'ADMIN' && item.isActive).length === 1)
    throw Object.assign(new Error('Conflict'), { response: { status: 409 } })
  Object.assign(user, { role })
  return { ...user }
}

export async function updateUserStatusMock(id: string, isActive: boolean): Promise<UserDto> {
  mockStatus('status')
  const user = mockUsers.find((item) => item.id === id)
  if (!user) throw Object.assign(new Error('Not found'), { response: { status: 404 } })
  if (user.role === 'ADMIN' && user.isActive && !isActive && mockUsers.filter((item) => item.role === 'ADMIN' && item.isActive).length === 1)
    throw Object.assign(new Error('Conflict'), { response: { status: 409 } })
  Object.assign(user, { isActive })
  return { ...user }
}

export function getMockManagedUsers() { return mockUsers }
