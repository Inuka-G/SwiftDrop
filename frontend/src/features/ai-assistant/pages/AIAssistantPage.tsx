import { type FormEvent, useState } from 'react'
import { aiAssistantApi } from '../api/aiAssistant.api'
import type { ChatMessage } from '../types/aiAssistant.types'
import './AIAssistantPage.css'

const starterMessage: ChatMessage = {
  id: 1,
  role: 'assistant',
  text: 'Hi! I can help estimate a delivery fee or track a SwiftDrop parcel.',
}

export function AIAssistantPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([starterMessage])
  const [input, setInput] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [error, setError] = useState('')

  async function sendMessage(event: FormEvent) {
    event.preventDefault()
    const message = input.trim()
    if (!message || isSending) return

    const userMessage: ChatMessage = {
      id: Date.now(),
      role: 'user',
      text: message,
    }

    setMessages((current) => [...current, userMessage])
    setInput('')
    setError('')
    setIsSending(true)

    try {
      const response = await aiAssistantApi.chat({ message })
      setMessages((current) => [
        ...current,
        {
          id: Date.now() + 1,
          role: 'assistant',
          text: response.message,
          toolsUsed: response.toolsUsed,
        },
      ])
    } catch {
      setError('The assistant is unavailable. Please check that the backend is running.')
    } finally {
      setIsSending(false)
    }
  }

  function usePrompt(prompt: string) {
    setInput(prompt)
  }

  return (
    <section className="ai-page">
      <div className="ai-heading">
        <p className="ai-eyebrow">SWIFTDROP AI</p>
        <h1>Delivery Assistant</h1>
        <p>Ask about parcel tracking or get a quick delivery-fee estimate.</p>
      </div>

      <div className="ai-prompts" aria-label="Example questions">
        <button type="button" onClick={() => usePrompt('Track parcel SD-1234567')}>
          Track a parcel
        </button>
        <button type="button" onClick={() => usePrompt('How much to send 2 kg to Colombo?')}>
          Estimate a fee
        </button>
      </div>

      <div className="ai-chat-card">
        <div className="ai-messages" aria-live="polite">
          {messages.map((message) => (
            <article key={message.id} className={`ai-message ai-message--${message.role}`}>
              <strong>{message.role === 'user' ? 'You' : 'SwiftDrop AI'}</strong>
              <p>{message.text}</p>
              {message.toolsUsed && message.toolsUsed.length > 0 && (
                <small>Live tool: {message.toolsUsed.join(', ')}</small>
              )}
            </article>
          ))}
          {isSending && <p className="ai-typing">SwiftDrop AI is thinking...</p>}
        </div>

        {error && <p className="ai-error" role="alert">{error}</p>}

        <form className="ai-form" onSubmit={sendMessage}>
          <label htmlFor="ai-message">Your question</label>
          <div className="ai-form-row">
            <input
              id="ai-message"
              value={input}
              onChange={(event) => setInput(event.target.value)}
              placeholder="Ask about a parcel or delivery fee..."
              maxLength={1000}
              disabled={isSending}
            />
            <button type="submit" disabled={isSending || !input.trim()}>
              Send
            </button>
          </div>
        </form>
      </div>
    </section>
  )
}

