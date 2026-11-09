import type { HubConnection } from '@microsoft/signalr'

export class SignalRConnectionManager {
  private connection: HubConnection | null = null

  setConnection(connection: HubConnection): void {
    this.connection = connection
  }

  async start(): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection has not been configured')
    }
    if (this.connection.state === 'Disconnected') {
      await this.connection.start()
    }
  }

  async stop(): Promise<void> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      await this.connection.stop()
    }
    this.connection = null
  }
}
