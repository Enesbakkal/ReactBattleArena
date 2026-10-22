import type { ReactNode } from 'react'
import s from './AuthLayout.module.scss'

type AuthLayoutProps = {
  title: string
  subtitle?: string
  children: ReactNode
}

function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
  return (
    <div className={s.root}>
      <div className={s.card}>
        <h1 className={s.title}>{title}</h1>
        {subtitle && <p className={s.subtitle}>{subtitle}</p>}
        <div className={s.body}>{children}</div>
      </div>
      <p className={s.footer}>{new Date().getFullYear()} © Battle Arena</p>
    </div>
  )
}

export default AuthLayout
// title kartın başlığıdır. children gövdedir. ReactNode o araya element, metin veya boşluk konabileceğini söyler. Yıl her çizimde tarayıcının tarihinden gelir. API'ye gitmez.