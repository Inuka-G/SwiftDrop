import axios from 'axios'
import { type FormEvent, type KeyboardEvent, useEffect, useRef, useState } from 'react'
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
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const formRef = useRef<HTMLFormElement>(null)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isSending, error])

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
    } catch (requestError) {
      if (axios.isAxiosError(requestError) && !requestError.response) {
        setError('Cannot reach SwiftDrop. Check that the backend is running on port 5017.')
      } else if (axios.isAxiosError(requestError) &&
                 (requestError.response?.status === 429 || requestError.response?.status === 503)) {
        setError('Gemini is temporarily busy. Please wait a moment and try again.')
      } else {
        setError('Something went wrong while sending your message. Please try again.')
      }
    } finally {
      setIsSending(false)
    }
  }

  function applyPrompt(prompt: string) {
    setInput(prompt)
  }

  function clearChat() {
    setMessages([starterMessage])
    setInput('')
    setError('')
  }

  function handleComposerKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      formRef.current?.requestSubmit()
    }
  }

  function renderMessage(text: string) {
    const bulletParts = text.split(/\s*\*\s+/).filter(Boolean)
    if (bulletParts.length > 1) {
      return (
        <>
          <p>{bulletParts[0]}</p>
          <ul>{bulletParts.slice(1).map((part) => <li key={part}>{part}</li>)}</ul>
        </>
      )
    }

    return text.split('\n').filter(Boolean).map((line) => <p key={line}>{line}</p>)
  }

  function badgeFor(message: ChatMessage) {
    const tool = message.toolsUsed?.[0]
    if (tool === 'EstimateFee') return '💰 Live pricing'
    if (tool === 'TrackParcel') return '📍 Live tracking'
    return message.role === 'assistant' && message.id !== starterMessage.id ? '✨ Gemini' : null
  }

  return (
    <section className="ai-page">
      <div className="ai-heading">
        <p className="ai-eyebrow">SWIFTDROP AI</p>
        <h1>Delivery Assistant</h1>
        <p>Ask about parcel tracking or get a quick delivery-fee estimate.</p>
      </div>

      <div className="ai-prompts" aria-label="Example questions">
        <button type="button" onClick={() => applyPrompt('Track parcel SD-1234567')}>
          <span>📍</span> Track a parcel
        </button>
        <button type="button" onClick={() => applyPrompt('How much to send 2 kg to Colombo?')}>
          <span>💰</span> Estimate a fee
        </button>
        <button type="button" onClick={() => applyPrompt('What can SwiftDrop help me with?')}>
          <span>✨</span> Ask Gemini
        </button>
      </div>

      <div className="ai-chat-card">
        <div className="ai-chat-toolbar">
          <span><i aria-hidden="true" /> Assistant online</span>
          <button type="button" onClick={clearChat} disabled={messages.length === 1 && !error}>
            Clear chat
          </button>
        </div>
        <div className="ai-messages" aria-live="polite">
          {messages.map((message) => (
            <article key={message.id} className={`ai-message ai-message--${message.role}`}>
              <strong>{message.role === 'user' ? 'You' : 'SwiftDrop AI'}</strong>
              <div className="ai-message-content">{renderMessage(message.text)}</div>
              {badgeFor(message) && <small className="ai-source-badge">{badgeFor(message)}</small>}
            </article>
          ))}
          {isSending && (
            <article className="ai-message ai-message--assistant ai-thinking" aria-label="SwiftDrop AI is thinking">
              <strong>SwiftDrop AI</strong>
              <span /><span /><span />
            </article>
          )}
          <div ref={messagesEndRef} />
        </div>

        {error && <p className="ai-error" role="alert">{error}</p>}

        <form ref={formRef} className="ai-form" onSubmit={sendMessage}>
          <label htmlFor="ai-message">Your question</label>
          <div className="ai-form-row">
            <textarea
              id="ai-message"
              value={input}
              onChange={(event) => setInput(event.target.value)}
              onKeyDown={handleComposerKeyDown}
              placeholder="Ask about a parcel or delivery fee..."
              maxLength={1000}
              rows={2}
              disabled={isSending}
            />
            <button type="submit" disabled={isSending || !input.trim()}>
              Send
            </button>
          </div>
          <small className="ai-input-hint">Enter to send · Shift + Enter for a new line</small>
        </form>
      </div>
    </section>
  )
}

