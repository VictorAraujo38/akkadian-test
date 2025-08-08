import React from 'react'
import styles from './Layout.module.css'

export const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear()

  return (
    <footer className={styles.footer}>
      <div className={styles.footerContent}>
        <p className={styles.copyright}>
          &copy; {currentYear} Medical Scheduling. Todos os direitos reservados.
        </p>
        <div className={styles.footerLinks}>
          <a href="#" className={styles.footerLink}>Termos de Uso</a>
          <a href="#" className={styles.footerLink}>Política de Privacidade</a>
          <a href="#" className={styles.footerLink}>Contato</a>
        </div>
      </div>
    </footer>
  )
}