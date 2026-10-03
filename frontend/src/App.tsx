import { BrowserRouter, Navigate, NavLink, Outlet, Route, Routes } from 'react-router'
import { useAuth, useMe } from './auth/context'
import { ChatsLayout, NoChatSelected } from './chats/ChatsLayout'
import { ChatView } from './chats/ChatView'
import { AuthPage } from './pages/AuthPage'
import { BotsPage } from './pages/BotsPage'
import { ProfilePage, WelcomePage } from './pages/ProfilePage'
import { TokensPage } from './pages/TokensPage'

function navClass({ isActive }: { isActive: boolean }) {
  return isActive ? 'nav-link active' : 'nav-link'
}

function AppLayout() {
  const me = useMe()
  const { logout } = useAuth()

  if (me.profile == null) return <WelcomePage />

  return (
    <div className="app">
      <header className="topbar">
        <span className="brand">Messenger</span>
        <nav className="row">
          <NavLink to="/chats" className={navClass}>
            Чаты
          </NavLink>
          <NavLink to="/bots" className={navClass}>
            Боты
          </NavLink>
          <NavLink to="/tokens" className={navClass}>
            Токены
          </NavLink>
          <NavLink to="/profile" className={navClass}>
            {me.profile.displayName}
          </NavLink>
        </nav>
        <button type="button" className="button small ghost" onClick={logout}>
          Выйти
        </button>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}

function RequireAuth() {
  const { status, reloadMe, logout } = useAuth()

  if (status === 'signedOut') return <Navigate to="/login" replace />
  if (status === 'loading') return <p className="muted center pad">Загрузка…</p>
  if (status === 'failed') {
    return (
      <div className="empty-state">
        <p className="error">Не удалось связаться с сервером.</p>
        <div className="row">
          <button type="button" className="button" onClick={() => void reloadMe()}>
            Повторить
          </button>
          <button type="button" className="button ghost" onClick={logout}>
            Выйти
          </button>
        </div>
      </div>
    )
  }
  return <AppLayout />
}

function AnonymousOnly() {
  const { status } = useAuth()
  return status === 'signedOut' ? <Outlet /> : <Navigate to="/chats" replace />
}

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AnonymousOnly />}>
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/register" element={<AuthPage mode="register" />} />
        </Route>
        <Route element={<RequireAuth />}>
          <Route path="/chats" element={<ChatsLayout />}>
            <Route index element={<NoChatSelected />} />
            <Route path=":chatId" element={<ChatView />} />
          </Route>
          <Route path="/bots" element={<BotsPage />} />
          <Route path="/tokens" element={<TokensPage />} />
          <Route path="/profile" element={<ProfilePage />} />
        </Route>
        <Route path="*" element={<Navigate to="/chats" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
