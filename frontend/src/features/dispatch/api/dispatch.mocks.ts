import type { AssignmentDto, CourierDto, CourierPageDto, UnassignedParcelDto, UnassignedParcelPageDto } from '../types/dispatch.types'

const baseTime = Date.now()
const zones = ['90c10000-0000-4000-8000-000000000001', '90c10000-0000-4000-8000-000000000002', '90c10000-0000-4000-8000-000000000003', '90c10000-0000-4000-8000-000000000004']

const parcels: UnassignedParcelDto[] = [
  ['48213', 18, 0, 'SMALL', 0.8], ['48214', 45, 0, 'MEDIUM', 2.4], ['48218', 60, 1, 'LARGE', 5.1],
  ['48225', 120, 2, 'SMALL', 1.2], ['48230', 25, 1, 'MEDIUM', 3.2], ['48239', 95, 3, 'LARGE', 6.4],
  ['48244', 14, 0, 'SMALL', 0.5], ['48251', 52, 2, 'MEDIUM', 1.9], ['48258', 32, 1, 'SMALL', 0.9],
  ['48263', 8, 0, 'LARGE', 4.2], ['48271', 78, 2, 'MEDIUM', 2.1], ['48278', 135, 3, 'SMALL', 0.7],
].map(([tracking, minutes, zoneIndex, size, weight]) => ({
  parcelId: `a1000000-0000-4000-8000-${String(tracking).padStart(12, '0')}`,
  trackingNumber: `SD-${tracking}`,
  zoneId: zones[Number(zoneIndex)],
  createdAt: new Date(baseTime - Number(minutes) * 60_000).toISOString(),
  sizeCategory: String(size),
  weightKg: Number(weight),
}))

export const mockCouriers: CourierDto[] = [
  { courierId: 'b1000000-0000-4000-8000-000000000001', name: 'Nimal Perera', zoneId: zones[0], isAvailable: true, vehicleType: 'Motorbike', licensePlate: null, activeDeliveryCount: 1 },
  { courierId: 'b1000000-0000-4000-8000-000000000002', name: 'Sunimal Silva', zoneId: zones[0], isAvailable: true, vehicleType: 'Three-Wheeler', licensePlate: null, activeDeliveryCount: 2 },
  { courierId: 'b1000000-0000-4000-8000-000000000003', name: 'Nuwan Fernando', zoneId: zones[1], isAvailable: true, vehicleType: 'Motorbike', licensePlate: null, activeDeliveryCount: 2 },
  { courierId: 'b1000000-0000-4000-8000-000000000004', name: 'Kamal Perera', zoneId: zones[0], isAvailable: true, vehicleType: 'Light Van', licensePlate: null, activeDeliveryCount: 3 },
  { courierId: 'b1000000-0000-4000-8000-000000000005', name: 'Ruwan Jayasinghe', zoneId: zones[2], isAvailable: true, vehicleType: 'Light Van', licensePlate: null, activeDeliveryCount: 4 },
  { courierId: 'b1000000-0000-4000-8000-000000000006', name: 'Kasun Bandara', zoneId: zones[0], isAvailable: false, vehicleType: 'Motorbike', licensePlate: null, activeDeliveryCount: 5 },
  { courierId: 'b1000000-0000-4000-8000-000000000007', name: 'Roshan Lokuge', zoneId: zones[3], isAvailable: false, vehicleType: 'Three-Wheeler', licensePlate: null, activeDeliveryCount: 0 },
]

export function getMockUnassigned(filters: { from: string; to: string }, page: number, pageSize: number): UnassignedParcelPageDto {
  const from = filters.from ? new Date(filters.from).getTime() : Number.NEGATIVE_INFINITY
  const to = filters.to ? new Date(filters.to).getTime() : Number.POSITIVE_INFINITY
  const filtered = parcels.filter((parcel) => {
    const created = new Date(parcel.createdAt).getTime()
    return created >= from && created <= to
  })
  return { items: filtered.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: filtered.length }
}

export function getMockCourierPage(page: number, pageSize: number): CourierPageDto {
  return { items: mockCouriers.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: mockCouriers.length }
}

export function assignMockParcel(parcelId: string, courierId: string): AssignmentDto {
  const parcelIndex = parcels.findIndex((parcel) => parcel.parcelId === parcelId)
  if (parcelIndex < 0) throw Object.assign(new Error('Parcel is no longer in the queue.'), { response: { status: 409 } })
  const courier = mockCouriers.find((candidate) => candidate.courierId === courierId)
  if (!courier) throw Object.assign(new Error('Courier was not found.'), { response: { status: 404 } })
  if (!courier.isAvailable) throw Object.assign(new Error('Courier is unavailable.'), { response: { status: 400 } })
  parcels.splice(parcelIndex, 1)
  courier.activeDeliveryCount += 1
  return { parcelId, courierId, assignedBy: 'b1000000-0000-4000-8000-000000000000', assignedAt: new Date().toISOString() }
}
