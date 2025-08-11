import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useRouter } from 'next/router'
import { appointmentService } from '@/services/api'
import { toast } from 'react-toastify'
import { CalendarIcon, ClockIcon, DocumentTextIcon } from '@heroicons/react/24/outline'
import styles from './AppointmentForm.module.css'
import { LoadingSpinner } from '@/components/Common/LoadingSpinner'

type AppointmentFormData = {
    appointmentDate: string
    appointmentTime: string
    symptoms: string
    additionalNotes?: string
}

const timeSlots = [
    '08:00', '09:00', '10:00', '11:00',
    '14:00', '15:00', '16:00', '17:00'
]

export const AppointmentForm: React.FC = () => {
    const router = useRouter()
    const [loading, setLoading] = useState(false)
    const [selectedTime, setSelectedTime] = useState('')
    const [triageResult, setTriageResult] = useState<string | null>(null)

    const {
        register,
        handleSubmit,
        watch,
        formState: { errors },
    } = useForm<AppointmentFormData>()

    const symptoms = watch('symptoms')

    const onSubmit = async (data: AppointmentFormData) => {
        setLoading(true)
        try {
            const appointmentDateTime = new Date(`${data.appointmentDate}T${selectedTime || data.appointmentTime}`)

            const response = await appointmentService.createAppointment({
                appointmentDate: appointmentDateTime.toISOString(),
                symptoms: data.symptoms,
                additionalNotes: data.additionalNotes,
            })

            toast.success('Agendamento criado com sucesso!')
            router.push('/patient/dashboard')
        } catch (error: any) {
            toast.error(error.message || 'Erro ao criar agendamento')
        } finally {
            setLoading(false)
        }
    }

    const handleSymptomAnalysis = async () => {
        if (!symptoms || symptoms.length < 10) {
            toast.warning('Descreva seus sintomas com pelo menos 10 caracteres')
            return
        }

        try {
            const response = await appointmentService.getTriageRecommendation(symptoms)
            setTriageResult(response.recommendedSpecialty)
            toast.success(`Especialidade recomendada: ${response.recommendedSpecialty}`)
        } catch (error) {
            toast.error('Erro ao analisar sintomas')
        }
    }

    // Set minimum date to tomorrow
    const tomorrow = new Date()
    tomorrow.setDate(tomorrow.getDate() + 1)
    const minDate = tomorrow.toISOString().split('T')[0]

    // Set maximum date to 30 days from now
    const maxDate = new Date()
    maxDate.setDate(maxDate.getDate() + 30)
    const maxDateStr = maxDate.toISOString().split('T')[0]

    return (
        <div className={styles.container}>
            <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
                <h2 className={styles.title}>Novo Agendamento</h2>

                <div className={styles.section}>
                    <h3 className={styles.sectionTitle}>
                        <CalendarIcon className="h-5 w-5" />
                        Data e Horário
                    </h3>

                    <div className={styles.dateTimeGrid}>
                        <div className={styles.inputGroup}>
                            <label className={styles.label}>Data do agendamento</label>
                            <input
                                {...register('appointmentDate', { required: 'Data é obrigatória' })}
                                type="date"
                                min={minDate}
                                max={maxDateStr}
                                className={styles.input}
                            />
                            {errors.appointmentDate && (
                                <span className={styles.error}>{errors.appointmentDate.message}</span>
                            )}
                        </div>

                        <div className={styles.inputGroup}>
                            <label className={styles.label}>Horário disponível</label>
                            <div className={styles.timeSlots}>
                                {timeSlots.map((time) => (
                                    <button
                                        key={time}
                                        type="button"
                                        onClick={() => setSelectedTime(time)}
                                        className={`${styles.timeSlot} ${selectedTime === time ? styles.timeSlotSelected : ''
                                            }`}
                                    >
                                        <ClockIcon className="h-4 w-4" />
                                        {time}
                                    </button>
                                ))}
                            </div>
                            <input
                                type="hidden"
                                {...register('appointmentTime', {
                                    required: 'Horário é obrigatório',
                                    validate: () => selectedTime !== '' || 'Selecione um horário'
                                })}
                                value={selectedTime}
                            />
                            {errors.appointmentTime && (
                                <span className={styles.error}>{errors.appointmentTime.message}</span>
                            )}
                        </div>
                    </div>
                </div>

                <div className={styles.section}>
                    <h3 className={styles.sectionTitle}>
                        <DocumentTextIcon className="h-5 w-5" />
                        Informações Médicas
                    </h3>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Descreva seus sintomas</label>
                        <textarea
                            {...register('symptoms', {
                                required: 'Sintomas são obrigatórios',
                                minLength: {
                                    value: 10,
                                    message: 'Descreva os sintomas com pelo menos 10 caracteres',
                                },
                            })}
                            rows={4}
                            className={styles.textarea}
                            placeholder="Ex: Dor de cabeça frequente, febre há 2 dias, dor no peito ao respirar..."
                        />
                        {errors.symptoms && (
                            <span className={styles.error}>{errors.symptoms.message}</span>
                        )}

                        <button
                            type="button"
                            onClick={handleSymptomAnalysis}
                            className={styles.analyzeButton}
                            disabled={!symptoms || symptoms.length < 10}
                        >
                            Analisar Sintomas com IA
                        </button>

                        {triageResult && (
                            <div className={styles.triageResult}>
                                <strong>Especialidade Recomendada:</strong> {triageResult}
                            </div>
                        )}
                    </div>

                    <div className={styles.inputGroup}>
                        <label className={styles.label}>Observações adicionais (opcional)</label>
                        <textarea
                            {...register('additionalNotes')}
                            rows={3}
                            className={styles.textarea}
                            placeholder="Informações adicionais que possam ser relevantes..."
                        />
                    </div>
                </div>

                <div className={styles.actions}>
                    <button
                        type="button"
                        onClick={() => router.back()}
                        className={styles.cancelButton}
                    >
                        Cancelar
                    </button>
                    <button
                        type="submit"
                        disabled={loading}
                        className={styles.submitButton}
                    >
                        {loading ? <LoadingSpinner size="small" /> : 'Confirmar Agendamento'}
                    </button>
                </div>
            </form>
        </div>
    )
}