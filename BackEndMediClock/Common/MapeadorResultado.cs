using Microsoft.AspNetCore.Mvc;

namespace BackEndMediClock.Common
{
    public static class MapeadorResultado
    {
        public static ActionResult DeResultado<T>(Resultado<T> resultado)
        {
            if (resultado.IsValid)
            {
                return new OkObjectResult(resultado.Value);
            }

            return ConvertirError(resultado);
        }

        public static ActionResult DeResultado(Resultado resultado)
        {
            if (resultado.IsValid)
            {
                return new NoContentResult();
            }

            return ConvertirError(resultado);
        }

        private static ObjectResult ConvertirError(Resultado resultado) => resultado.Tipo switch
        {
            TipoResultado.NoEncontrado => new NotFoundObjectResult(resultado.Errors),
            TipoResultado.Conflicto => new ConflictObjectResult(resultado.Errors),
            _ => new BadRequestObjectResult(resultado.Errors)
        };
    }
}