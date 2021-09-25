#ifndef __MS5837_H
#define __MS5837_H
#include "stm32f4xx.h" 
#include "usart.h"

typedef struct 
{
	int8_t T;			//�ַ�T
	int8_t Fe;			//�ַ�= F��ʾFirst e��ʾequal
	int16_t Ftemp;		//���������¶�ֵF��ʾС����ǰ�벿
	int8_t fdot;		//С���� f��ʾfirst��dot��ʾ'.'
	int16_t Btemp;		//���������¶�ֵB��ʾС�����벿
	int8_t D;			//�ַ�D
	int8_t Se;			//�ַ�= S��ʾSecond e��ʾequal
	int16_t Fpre;		//�������������ֵF��ʾС����ǰ�벿
	int8_t sdot;		//s��ʾsecond 
	int16_t Bpre;		//�������������ֵB��ʾС�����벿
	int8_t r;			//\r
	int8_t n;			//\n
}ms5837RxData_t;
 





void GetMs5837Data(uint8_t data);
void  OutputMS5837(void);
void ProcessMS5837Data(void);
void pritdata(void);

#endif
