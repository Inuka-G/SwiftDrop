
import { useCallback, useEffect, useState } from 'react'
import axios from 'axios'
import { getErrorMessage } from '../../../lib/axios'
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
        catch (requestError) {
            setParcels([])
            setError(
                axios.isAxiosError(requestError) && requestError.response?.status === 401
                    ? 'Your session has expired. Please sign in again.'
                    : getErrorMessage(requestError, 'Unable to load your deliveries. Please try again.'),
            )
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
