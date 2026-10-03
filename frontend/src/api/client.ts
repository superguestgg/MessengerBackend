import createClient, { type Middleware } from 'openapi-fetch'
import type { components, paths } from './schema'

// Types come from the backend's Swagger: regenerate with `npm run api:generate`.
type Schemas = components['schemas']
export type Me = Schemas['MeResult']
export type Chat = Schemas['ChatResult']
export type ChatMember = Schemas['ChatMemberResult']
export type ChatRole = Schemas['ChatRole']
export type Message = Schemas['MessageResult']
export type Bot = Schemas['BotResult']
export type AccessToken = Schemas['AccessTokenResult']
export type LoginResult = Schemas['LoginResult']
export type UserSearchResult = Schemas['UserSearchResult']

// Empty means the API is served from the same origin as the frontend.
export const apiUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/+$/, '')

export const api = createClient<paths>({ baseUrl: apiUrl })

let accessToken: string | null = null
let onUnauthorized: (() => void) | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler
}

const auth: Middleware = {
  onRequest({ request }) {
    if (accessToken != null) {
      request.headers.set('Authorization', `Bearer ${accessToken}`)
    }
    return request
  },
  onResponse({ request, response }) {
    // A rejected token means the session is over (JWTs can't be refreshed yet).
    if (response.status === 401 && request.headers.has('Authorization')) {
      onUnauthorized?.()
    }
    return response
  },
}

api.use(auth)

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface Problem {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

const statusMessages: Record<number, string> = {
  401: 'Нужно войти заново',
  403: 'Недостаточно прав',
  404: 'Не найдено',
  409: 'Конфликт: данные изменились, попробуйте ещё раз',
  429: 'Слишком много запросов, подождите немного',
}

function problemMessage(status: number, body: unknown): string {
  const problem = (typeof body === 'object' && body != null ? body : {}) as Problem
  if (problem.errors != null) {
    const messages = Object.values(problem.errors).flat()
    if (messages.length > 0) return messages.join(' ')
  }
  if (problem.detail != null && problem.detail !== '') return problem.detail
  return statusMessages[status] ?? problem.title ?? `Ошибка сервера (${status})`
}

interface FetchResult<T> {
  data?: T
  error?: unknown
  response: Response
}

// Turns an openapi-fetch result into data or an ApiError.
export async function unwrap<T>(request: Promise<FetchResult<T>>): Promise<T> {
  let result: FetchResult<T>
  try {
    result = await request
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(0, 'Нет связи с сервером')
  }

  const { data, error, response } = result
  if (!response.ok) {
    throw new ApiError(response.status, problemMessage(response.status, error))
  }
  return data as T
}

export function errorMessage(error: unknown): string {
  if (error instanceof Error) return error.message
  return 'Что-то пошло не так'
}

export function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}
