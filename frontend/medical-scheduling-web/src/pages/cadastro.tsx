import { useState, useEffect } from 'react'
import { useRouter } from 'next/router'
import Link from 'next/link'
import { useForm } from 'react-hook-form'
import { useAuth } from '@/contexts/AuthContext'
import { api } from '@/services/api'
import { toast } from 'react-toastify'

type RegisterForm = {
  name: string
  email: string
  password: string
  confirmPassword: string
  role: string
  // Campos para médico
  crmNumber?: string
  phone?: string
  primarySpecialtyId?: number
  secondarySpecialtyIds?: number[]
}

export default function Cadastro() {
  const router = useRouter()
  const { register: registerUser } = useAuth()
  const [loading, setLoading] = useState(false)
  const [specialties, setSpecialties] = useState<any[]>([])
  const [selectedSecondarySpecialties, setSelectedSecondarySpecialties] = useState<number[]>([])

  const { register, handleSubmit, watch, formState: { errors } } = useForm<RegisterForm>()

  const password = watch('password')
  const selectedRole = watch('role')

  // Buscar especialidades quando component monta
  useEffect(() => {
    fetchSpecialties()
  }, [])

  const fetchSpecialties = async () => {
    try {
      const response = await api.get('/specialties')
      setSpecialties(response.data)
    } catch (error) {
      console.error('Erro ao buscar especialidades:', error)
    }
  }

  const onSubmit = async (data: RegisterForm) => {
    setLoading(true)
    try {
      // Primeiro, registrar o usuário
      await registerUser({
        name: data.name,
        email: data.email,
        password: data.password,
        role: data.role,
        ...(data.role === 'doctor' && {
          crmNumber: data.crmNumber,
          phone: data.phone
        })
      })

      // Se for médico, registrar especialidades
      if (data.role === 'doctor' && data.primarySpecialtyId) {
        try {
          // Registrar especialidade primária
          await api.post('/doctor-specialties', {
            specialtyId: data.primarySpecialtyId,
            isPrimary: true,
            licenseNumber: data.crmNumber,
            certificationDate: new Date().toISOString()
          })

          // Registrar especialidades secundárias
          for (const specialtyId of selectedSecondarySpecialties) {
            await api.post('/doctor-specialties', {
              specialtyId: specialtyId,
              isPrimary: false,
              licenseNumber: data.crmNumber,
              certificationDate: new Date().toISOString()
            })
          }

          toast.success('Médico cadastrado com especialidades!')
        } catch (specialtyError) {
          toast.warning('Usuário criado, mas houve erro ao registrar especialidades. Você pode atualizá-las no perfil.')
        }
      } else {
        toast.success('Cadastro realizado com sucesso!')
      }

      router.push('/')
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Erro ao cadastrar'
      toast.error(errorMessage)
    } finally {
      setLoading(false)
    }
  }

  const handleSecondarySpecialtyChange = (specialtyId: number, checked: boolean) => {
    if (checked) {
      setSelectedSecondarySpecialties([...selectedSecondarySpecialties, specialtyId])
    } else {
      setSelectedSecondarySpecialties(selectedSecondarySpecialties.filter(id => id !== specialtyId))
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Crie sua conta
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Ou{' '}
            <Link href="/login" className="font-medium text-blue-600 hover:text-blue-500">
              faça login se já tem uma conta
            </Link>
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)}>
          <div className="space-y-4">
            {/* Nome */}
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-gray-700">
                Nome completo
              </label>
              <input
                {...register('name', { required: 'Nome é obrigatório' })}
                type="text"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="Nome completo"
              />
              {errors.name && (
                <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>
              )}
            </div>

            {/* Email */}
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                Email
              </label>
              <input
                {...register('email', {
                  required: 'Email é obrigatório',
                  pattern: {
                    value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                    message: 'Email inválido'
                  }
                })}
                type="email"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="Email"
              />
              {errors.email && (
                <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>
              )}
            </div>

            {/* Tipo de usuário */}
            <div>
              <label htmlFor="role" className="block text-sm font-medium text-gray-700">
                Tipo de usuário
              </label>
              <select
                {...register('role', { required: 'Selecione o tipo de usuário' })}
                className="mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md"
              >
                <option value="">Selecione...</option>
                <option value="patient">👤 Paciente</option>
                <option value="doctor">👨‍⚕️ Médico</option>
              </select>
              {errors.role && (
                <p className="mt-1 text-sm text-red-600">{errors.role.message}</p>
              )}
            </div>

            {/* Campos específicos para médico */}
            {selectedRole === 'doctor' && (
              <div className="space-y-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                <h3 className="text-lg font-medium text-blue-900 mb-3">
                  👨‍⚕️ Informações do Médico
                </h3>

                {/* CRM */}
                <div>
                  <label htmlFor="crmNumber" className="block text-sm font-medium text-gray-700">
                    CRM *
                  </label>
                  <input
                    {...register('crmNumber', {
                      required: selectedRole === 'doctor' ? 'CRM é obrigatório para médicos' : false
                    })}
                    type="text"
                    placeholder="Ex: CRM/SP 123456"
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  />
                  {errors.crmNumber && (
                    <p className="mt-1 text-sm text-red-600">{errors.crmNumber.message}</p>
                  )}
                </div>

                {/* Telefone */}
                <div>
                  <label htmlFor="phone" className="block text-sm font-medium text-gray-700">
                    Telefone
                  </label>
                  <input
                    {...register('phone')}
                    type="tel"
                    placeholder="(11) 99999-9999"
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  />
                </div>

                {/* Especialidade Primária */}
                <div>
                  <label htmlFor="primarySpecialtyId" className="block text-sm font-medium text-gray-700">
                    Especialidade Principal *
                  </label>
                  <select
                    {...register('primarySpecialtyId', {
                      required: selectedRole === 'doctor' ? 'Especialidade principal é obrigatória' : false
                    })}
                    className="mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md"
                  >
                    <option value="">Selecione sua especialidade principal...</option>
                    {specialties.map((specialty) => (
                      <option key={specialty.id} value={specialty.id}>
                        {specialty.name} ({specialty.department})
                      </option>
                    ))}
                  </select>
                  {errors.primarySpecialtyId && (
                    <p className="mt-1 text-sm text-red-600">{errors.primarySpecialtyId.message}</p>
                  )}
                </div>

                {/* Especialidades Secundárias */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Especialidades Secundárias (opcional)
                  </label>
                  <div className="max-h-32 overflow-y-auto space-y-2 border border-gray-200 rounded-md p-2">
                    {specialties.map((specialty) => (
                      <label key={specialty.id} className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          checked={selectedSecondarySpecialties.includes(specialty.id)}
                          onChange={(e) => handleSecondarySpecialtyChange(specialty.id, e.target.checked)}
                          className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                        />
                        <span className="text-sm text-gray-700">
                          {specialty.name} ({specialty.department})
                        </span>
                      </label>
                    ))}
                  </div>
                  <p className="mt-1 text-xs text-gray-500">
                    Selecione outras especialidades que você atende
                  </p>
                </div>
              </div>
            )}

            {/* Senhas */}
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                Senha
              </label>
              <input
                {...register('password', {
                  required: 'Senha é obrigatória',
                  minLength: {
                    value: 6,
                    message: 'Senha deve ter no mínimo 6 caracteres'
                  }
                })}
                type="password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="Senha"
              />
              {errors.password && (
                <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700">
                Confirmar senha
              </label>
              <input
                {...register('confirmPassword', {
                  required: 'Confirmação de senha é obrigatória',
                  validate: value => value === password || 'As senhas não coincidem'
                })}
                type="password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="Confirmar senha"
              />
              {errors.confirmPassword && (
                <p className="mt-1 text-sm text-red-600">{errors.confirmPassword.message}</p>
              )}
            </div>
          </div>

          <div>
            <button
              type="submit"
              disabled={loading}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
            >
              {loading ? 'Cadastrando...' : selectedRole === 'doctor' ? '👨‍⚕️ Cadastrar Médico' : 'Cadastrar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}