export const validators = {
  email: (email: string) => {
    const re = /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i;
    return re.test(email) || "Email inválido";
  },

  password: (password: string) => {
    if (password.length < 6) return "Senha deve ter no mínimo 6 caracteres";
    if (!/[A-Z]/.test(password))
      return "Senha deve conter pelo menos uma letra maiúscula";
    if (!/[a-z]/.test(password))
      return "Senha deve conter pelo menos uma letra minúscula";
    if (!/[0-9]/.test(password))
      return "Senha deve conter pelo menos um número";
    return true;
  },

  cpf: (cpf: string) => {
    const cleaned = cpf.replace(/\D/g, "");
    if (cleaned.length !== 11) return "CPF deve ter 11 dígitos";

    // Validação do CPF
    let sum = 0;
    let remainder;

    if (cleaned === "00000000000") return "CPF inválido";

    for (let i = 1; i <= 9; i++) {
      sum = sum + parseInt(cleaned.substring(i - 1, i)) * (11 - i);
    }

    remainder = (sum * 10) % 11;
    if (remainder === 10 || remainder === 11) remainder = 0;
    if (remainder !== parseInt(cleaned.substring(9, 10))) return "CPF inválido";

    sum = 0;
    for (let i = 1; i <= 10; i++) {
      sum = sum + parseInt(cleaned.substring(i - 1, i)) * (12 - i);
    }

    remainder = (sum * 10) % 11;
    if (remainder === 10 || remainder === 11) remainder = 0;
    if (remainder !== parseInt(cleaned.substring(10, 11)))
      return "CPF inválido";

    return true;
  },

  phone: (phone: string) => {
    const cleaned = phone.replace(/\D/g, "");
    if (cleaned.length < 10 || cleaned.length > 11) {
      return "Telefone inválido";
    }
    return true;
  },

  required: (value: unknown) => {
    return !!value || "Campo obrigatório";
  },

  minLength: (min: number) => (value: string) => {
    return value.length >= min || `Mínimo de ${min} caracteres`;
  },

  maxLength: (max: number) => (value: string) => {
    return value.length <= max || `Máximo de ${max} caracteres`;
  },
};
