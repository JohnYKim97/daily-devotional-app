import { Component, computed, input } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';

const LOGO_ASPECT_RATIO = 724 / 1024;

@Component({
  selector: 'app-lighthouse-icon',
  imports: [NgOptimizedImage],
  templateUrl: './lighthouse-icon.component.html',
  styleUrl: './lighthouse-icon.component.scss',
})
export class LighthouseIconComponent {
  size = input(24);
  protected width = computed(() => Math.round(this.size() * LOGO_ASPECT_RATIO));
}
