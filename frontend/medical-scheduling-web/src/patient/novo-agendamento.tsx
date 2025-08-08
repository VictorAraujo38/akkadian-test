import { useState } from 'react'
import { useRouter } from 'next/router'
import { useForm } from 'react-hook-form'

import { api } from '@/services/api'
import { toast } from 'react-toastify'
import { ArrowLeftIcon } from '@heroicons/react/24/outline'
import { withAuth } from '@/hooks/withAuth'

type AppointmentForm = {
  appointmentDate: string
  appointmentTime: string
  symptoms: string
}

function NovoAgendamento() {
  const router = useRouter()
  const [loading, setLoading] = useState(false)
  const { register, handleSubmit, formState: { errors } } = useForm<AppointmentForm>()

  const onSubmit = async (data: AppointmentForm) => {
    setLoading(true)
    try {
      const appointmentDateTime = new Date(`${data.appointmentDate}T${data.appointmentTime}`)
      
      await api.post('/paciente/agendamentos', {
        appointmentDate: appointmentDateTime.toISOString(),
        symptoms: data.symptoms
      })
      
      toast.success('Agendamento criado com sucesso!')
      router.push('/paciente/dashboard')
    } catch (error: unknown) {
      let errorMessage = 'Erro ao criar agendamento'
      if (error instanceof Error) {
        errorMessage = error.message
      }
      toast.error(errorMessage)
    } finally {
      setLoading(false)
    }
  }

  // Set minimum date to tomorrow
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  const minDate = tomorrow.toISOString().split('T')[0]

  return (
      <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <button
            onClick={() => router.push('/paciente/dashboard')}
            className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700"
          >
            <ArrowLeftIcon className="h-4 w-4 mr-2" />
            Voltar aos agendamentos
          </button>
        </div>

        <div className="bg-white shadow rounded-lg p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-6">Novo Agendamento</h1>
          
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
              <div>
                <label htmlFor="appointmentDate" className="block text-sm font-medium text-gray-700">
                  Data do agendamento
                </label>
                <input
                  {...register('appointmentDate', { required: 'Data é obrigatória' })}
                  type="date"
                  min={minDate}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                />
                {errors.appointmentDate && (
                  <p className="mt-1 text-sm text-red-600">{errors.appointmentDate.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="appointmentTime" className="block text-sm font-medium text-gray-700">
                  Horário
                </label>
                <select
                  {...register('appointmentTime', { required: 'Horário é obrigatório' })}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                >
                  <option value="">Selecione um horário</option>
                  <option value="08:00">08:00</option>
                  <option value="09:00">09:00</option>
                  <option value="10:00">10:00</option>
                  <option value="11:00">11:00</option>
                  <option value="14:00">14:00</option>
                  <option value="15:00">15:00</option>
                  <option value="16:00">16:00</option>
                  <option value="17:00">17:00</option>
                </select>
                {errors.appointmentTime && (
                  <p className="mt-1 text-sm text-red-600">{errors.appointmentTime.message}</p>
                )}
              </div>
            </div>

            <div>
              <label htmlFor="symptoms" className="block text-sm font-medium text-gray-700">
                Descreva seus sintomas
              </label>
              <textarea
                {...register('symptoms', { 
                  required: 'Sintomas são obrigatórios',
                  minLength: {
                    value: 10,
                    message: 'Descreva os sintomas com pelo menos 10 caracteres'
                  }
                })}
                rows={4}
                className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                placeholder="Ex: Dor de cabeça frequente, febre há 2 dias..."
              />
              {errors.symptoms && (
                <p className="mt-1 text-sm text-red-600">{errors.symptoms.message}</p>
              )}
              <p className="mt-2 text-sm text-gray-500">
                Seus sintomas serão analisados por IA para recomendar a especialidade médica adequada.
              </p>
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="submit"
                disabled={loading}
                className="inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                {loading ? 'Agendando...' : 'Agendar consulta'}
              </button>
            </div>
          </form>
        </div>
      </div>
    )
}

export default withAuth(NovoAgendamento, 'Patient')