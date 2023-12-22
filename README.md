# Maquina de fresco
## Proyecto ganador del concurso Temperature Warrior UC3M 2022 

### Qué es

Nuestro Temperature Warrior es un dispositivo de control de temperatura automático de bajo coste implementado en C# sobre .NET.

### Su composición

Una placa [Meadow](https://www.wildernesslabs.co/) se conecta al sensor de temperatura TMP36 y a dos actuadores: una placa peltier para enfriar y un secador de pelo para calentar. Gracias a un display TFT ST7789, el sistema muestra la temperatura en tiempo real. Además, mediante el servidor web incorporado, se pueden programar las pruebas propias de la competición.

![esquema_meadow](https://github.com/Jim-Hawkins/Maquina-de-fresco/assets/67739508/bb05909d-a6ec-4e1c-9ec6-47d6f0b7b919)

### Cómo funciona

1. El operador introduce las credenciales Wi-Fi en el sistema.
2. Cuando el sistema arranca su servidor interno, mostrará su IP y puerto para que el operador se conecte desde un navegador.
3. El operador rellena los datos de la prueba: rangos de temperatura, rangos de duración y tasa de refresco del medidor.
4. Cuando el resto de competidores están configurados, el operador activa el sistema y ¡comienza la ronda!

![Web_interface](https://github.com/Jim-Hawkins/Maquina-de-fresco/assets/67739508/0a121e35-88b5-44a6-b1f1-b891c691b4cb)

### El equipo

Este proyecto ganador no habría salido adelante sin el trabajo de Fabiana Calles, Sergio Gil, Sergio Tapia, Alejandro Salazar, Adrián Pérez, Miguel Ávila y Gonzalo Díaz.
