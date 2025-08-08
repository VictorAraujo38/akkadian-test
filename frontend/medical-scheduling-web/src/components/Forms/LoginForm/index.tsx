import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useRouter } from 'next/router'
import Link from 'next/link'
import { useAuth } from '@/contexts/AuthContext'
import { toast } from 'react-toastify'
import { EyeIcon, EyeSlashIcon, LockClosedIcon, EnvelopeIcon } from '@heroicons/react/24/outline'
import styles from './LoginForm.module.css'
import { LoadingSpinner } from '@/components/Common/LoadingSpinner'

type LoginFormData = {
    email: string
    password: string
    rememberMe: boolean
}

export const LoginForm: React.FC = () => {
    const router = useRouter()
    const { login } = useAuth()
    const [loading, setLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<LoginFormData>()

    const onSubmit = async (data: LoginFormData) => {
        setLoading(true)
        try {
            await login(data.email, data.password, data.rememberMe)
            toast.success('Login realizado com sucesso!')
            router.push('/')
        } catch (error: any) {
            toast.error(error.message || 'Erro ao fazer login')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className={styles.container}>
            <div className={styles.formWrapper}>
                <div className={styles.header}>
                    <div className={styles.logo}>
                        <LockClosedIcon className="h-12 w-12 text-blue-600" />
                    </div>
                    <h2 className={styles.title}>Faça login em sua conta</h2>
                    <p className={styles.subtitle}>
                        Ou{' '}
                        <Link href="/cadastro" className={styles.link}>
                            cadastre-se gratuitamente
                        </Link>
                    </p>
                </div>

                <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
                    <div className={styles.inputGroup}>
                        <label htmlFor="email" className={styles.label}>
                            Email
                        </label>
                        <div className={styles.inputWrapper}>
                            <EnvelopeIcon className={styles.inputIcon} />
                            <input
                                {...register('email', {
                                    required: 'Email é obrigatório',
                                    pattern: {
                                        value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                                        message: 'Email inválido',
                                    },
                                })}
                                type="email"
                                className={styles.input}
                                placeholder="seu@email.com"
                                autoComplete="email"
                            />
                        </div>
                        {errors.email && (
                            <span className={styles.error}>{errors.email.message}</span>
                        )}
                    </div>

                    <div className={styles.inputGroup}>
                        <label htmlFor="password" className={styles.label}>
                            Senha
                        </label>
                        <div className={styles.inputWrapper}>
                            <LockClosedIcon className={styles.inputIcon} />
                            <input
                                {...register('password', {
                                    required: 'Senha é obrigatória',
                                    minLength: {
                                        value: 6,
                                        message: 'Senha deve ter no mínimo 6 caracteres',
                                    },
                                })}
                                type={showPassword ? 'text' : 'password'}
                                className={styles.input}
                                placeholder="••••••••"
                                autoComplete="current-password"
                            />
                            <button
                                type="button"
                                onClick={() => setShowPassword(!showPassword)}
                                className={styles.passwordToggle}
                            >
                                {showPassword ? (
                                    <EyeSlashIcon className="h-5 w-5" />
                                ) : (
                                    <EyeIcon className="h-5 w-5" />
                                )}
                            </button>
                        </div>
                        {errors.password && (
                            <span className={styles.error}>{errors.password.message}</span>
                        )}
                    </div>

                    <div className={styles.rememberForgot}>
                        <label className={styles.checkbox}>
                            <input
                                {...register('rememberMe')}
                                type="checkbox"
                                className={styles.checkboxInput}
                            />
                            <span className={styles.checkboxLabel}>Lembrar de mim</span>
                        </label>
                        <Link href="/esqueci-senha" className={styles.forgotLink}>
                            Esqueceu a senha?
                        </Link>
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className={styles.submitButton}
                    >
                        {loading ? <LoadingSpinner size="small" /> : 'Entrar'}
                    </button>
                </form>

                <div className={styles.divider}>
                    <span className={styles.dividerText}>Ou continue com</span>
                </div>

                <div className={styles.socialButtons}>
                    <button className={styles.socialButton}>
                        <img src="/google-icon.svg" alt="Google" className="h-5 w-5" />
                        Google
                    </button>
                    <button className={styles.socialButton}>
                        <img src="/facebook-icon.svg" alt="Facebook" className="h-5 w-5" />
                        Facebook
                    </button>
                </div>
            </div>
        </div>
    )
}

export default LoginForm
