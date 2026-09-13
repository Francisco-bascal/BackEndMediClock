namespace BackEndMediClock.Common
{
    public enum TipoResultado
    {
        Exito,
        NoEncontrado,
        Conflicto,
        Validacion
    }

    public class Resultado
    {
        public bool IsValid { get; }
        public List<string> Errors { get; } = new List<string>();
        public TipoResultado Tipo { get; }

        protected Resultado(bool valido, TipoResultado tipo, List<string>? errores = null)
        {
            IsValid = valido;
            Tipo = tipo;
            Errors = errores ?? new List<string>();
        }

        public static Resultado Success() => new Resultado(true, TipoResultado.Exito);

        public static Resultado NoEncontrado(string error) =>
            new Resultado(false, TipoResultado.NoEncontrado, new List<string> { error });

        public static Resultado Conflicto(string error) =>
            new Resultado(false, TipoResultado.Conflicto, new List<string> { error });

        public static Resultado Validacion(string error) =>
            new Resultado(false, TipoResultado.Validacion, new List<string> { error });
    }

    public class Resultado<T> : Resultado
    {
        public T? Value { get; }

        protected Resultado(T? value, bool valido, TipoResultado tipo, List<string>? errores = null)
            : base(valido, tipo, errores)
        {
            Value = value;
        }

        public static Resultado<T> Success(T valor) => new Resultado<T>(valor, true, TipoResultado.Exito);

        public static new Resultado<T> NoEncontrado(string error) =>
            new Resultado<T>(default, false, TipoResultado.NoEncontrado, new List<string> { error });

        public static new Resultado<T> Conflicto(string error) =>
            new Resultado<T>(default, false, TipoResultado.Conflicto, new List<string> { error });

        public static new Resultado<T> Validacion(string error) =>
            new Resultado<T>(default, false, TipoResultado.Validacion, new List<string> { error });

        public static Resultado<T> Failure(string error) =>
            new Resultado<T>(default, false, TipoResultado.Validacion, new List<string> { error });

        public static Resultado<T> Failure(List<string> errores) =>
            new Resultado<T>(default, false, TipoResultado.Validacion, errores);
    }
}