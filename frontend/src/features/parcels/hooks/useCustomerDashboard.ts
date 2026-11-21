
import { useCallback, useEffect, useState } from 'react'
import { parcelsApi } from '../api/parcels.api'
import type { ParcelSummaryDto } from '../types/parcel.types'

interface CustomerDashboardData {
    parcels: ParcelSummaryDto[]
    isLoading: boolean
    error: string | null
    refresh: () => Promise<void>
}

export function useCustomerDashboard(): CustomerDashboardData {
    const [parcels, setParcels] = useState<ParcelSummaryDto[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

    const refresh = useCallback(async () => {
        setIsLoading(true)
        setError(null)

        try {
            const history = await parcelsApi.getMyHistory()
            setParcels(history)
        }
        catch {
            if (import.meta.env.DEV) {
                setParcels([
                    {
                        id: 'demo-001',
                        trackingNumber: 'SWD-2026-001',
                        recipientName: 'Kasun Perera',
                        dropoffAddress: 'Colombo 03',
                        weightKg: 2,
                        estimatedFee: 450,
                        currentStatus: 'In Transit',
                        createdAtUtc: new Date().toISOString(),
                    },
                    {
                        id: 'demo-002',
                        trackingNumber: 'SWD-2026-002',
                        recipientName: 'Nimal Silva',
                        dropoffAddress: 'Kandy',
                        weightKg: 1.5,
                        estimatedFee: 650,
                        currentStatus: 'Pending',
                        createdAtUtc: new Date(Date.now() - 86400000).toISOString(),
                    },
                    {
                        id: 'demo-003',
                        trackingNumber: 'SWD-2026-003',
                        recipientName: 'Amaya Fernando',
                        dropoffAddress: 'Galle',
                        weightKg: 3,
                        estimatedFee: 850,
                        currentStatus: 'Delivered',
                        createdAtUtc: new Date(Date.now() - 172800000).toISOString(),
                    },
                ])
                setError(null)
            } else {
                setError('Unable to load your deliveries. Please try again.')
            }
        } finally {
            setIsLoading(false)
        }
    }, [])

    useEffect(() => {
        void refresh()
    }, [refresh])

    return {
        parcels,
        isLoading,
        error,
        refresh,
    }
}
