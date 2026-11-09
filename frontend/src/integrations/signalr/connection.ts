import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

export function createSignalRConnection(accessToken?: string) {
  const hubUrl = import.meta.env.VITE_REALTIME_HUB_URL
  if (!hubUrl) {
    throw new Error('VITE_REALTIME_HUB_URL is not configured')
  }

  return new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => accessToken ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}
